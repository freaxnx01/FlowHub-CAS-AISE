#!/usr/bin/env node
// Automated layout check for the markdown→PDF docs — catches content that the
// print pipeline CLIPS at a box edge (a wide <pre>/code line, an un-wrappable
// token, an over-wide table or image). render.mjs exits 0 even when Chrome cuts
// such content off at the page edge, and the clip is invisible to a rasterised
// "is the text present?" check (the ink stops at the box, the margin stays white,
// poppler clamps the reported text bbox to the edge). The reliable place to see it
// is the live DOM: lay the document out at the exact print content width and ask
// the browser which elements overflow their box (scrollWidth > clientWidth) or run
// past the content width. This is what found the §5.4 "(…, FlowHub.Core)" clip
// that both pixel checks and a visual spot-check missed.
//
// Width model mirrors render.mjs exactly: A4 (210mm) minus left+right print
// margins → standard 210−18−18 = 174mm, --compact 210−14−14 = 182mm, at 96 CSS
// px/inch. Mermaid diagrams are rendered (offline, via render.mjs's preparePage)
// before measuring, so a wide diagram is judged on its scaled-to-fit size.
//
// Usage:
//   node check-layout.mjs <file.md> [more.md …] [--compact] [--tolerance 2]
// Exit code: 0 = no clipping, 1 = overflow found, 64 = bad invocation.

import fs from "node:fs/promises";
import path from "node:path";
import puppeteer from "puppeteer";
import { buildMarkdownIt, loadCss, wrapHtml, preparePage } from "./render.mjs";

const A4_WIDTH_MM = 210;
const MARGIN_X_MM = { standard: 18, compact: 14 };
const CSS_PX_PER_MM = 96 / 25.4;

function parseArgs(argv) {
  const opts = { files: [], variant: "standard", tol: 2 };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--compact") opts.variant = "compact";
    else if (a === "--tolerance") opts.tol = Number(argv[++i]);
    else opts.files.push(a);
  }
  if (opts.files.length === 0) {
    console.error("usage: check-layout.mjs <file.md> [more.md …] [--compact] [--tolerance 2]");
    process.exit(64);
  }
  return opts;
}

function contentPx(variant) {
  return Math.round((A4_WIDTH_MM - 2 * MARGIN_X_MM[variant]) * CSS_PX_PER_MM);
}

// Find elements whose content is wider than their box (will clip) or that extend
// past the page content width. Returns a de-duplicated, deepest-first list so a
// clipped <pre> is reported once, not also via its parent.
function auditFn(W, tol) {
  const sel = "pre, code, table, .mermaid svg, img, p, li, td, th, h1, h2, h3, h4, blockquote";
  const hits = [];
  for (const el of document.querySelectorAll(sel)) {
    const over = el.scrollWidth - el.clientWidth; // content wider than its own box
    const past = Math.round(el.getBoundingClientRect().right - W); // extends past page
    if (over > tol || past > tol) {
      hits.push({
        el,
        tag: el.tagName.toLowerCase() + (el.className ? "." + ("" + el.className).split(" ")[0] : ""),
        over: Math.max(0, Math.round(over)),
        past: Math.max(0, past),
        text: (el.textContent || "").trim().replace(/\s+/g, " ").slice(0, 80),
      });
    }
  }
  // Drop a hit if a descendant is also a hit (report the innermost offender).
  return hits
    .filter((h) => !hits.some((o) => o !== h && h.el.contains(o.el)))
    .map(({ el, ...rest }) => rest);
}

async function checkFile(browser, file, opts) {
  const md = buildMarkdownIt();
  const css = await loadCss();
  const body = md.render(await fs.readFile(file, "utf8"));
  const fullHtml = wrapHtml({ title: path.basename(file, ".md"), body, css, compact: opts.variant === "compact" });
  const W = contentPx(opts.variant);

  const page = await preparePage(browser, fullHtml);
  await page.setViewport({ width: W, height: 1400 });
  await page.emulateMediaType("print");
  const hits = await page.evaluate(auditFn, W, opts.tol);
  await page.close();
  return hits;
}

async function main() {
  const opts = parseArgs(process.argv.slice(2));
  const browser = await puppeteer.launch({ headless: true, args: ["--no-sandbox", "--disable-dev-shm-usage"] });
  let bad = 0;
  try {
    for (const file of opts.files) {
      let hits;
      try {
        hits = await checkFile(browser, file, opts);
      } catch (e) {
        console.error(`✗ ${path.basename(file)}: ${e.message}`);
        bad++;
        continue;
      }
      if (hits.length === 0) {
        console.error(`✓ ${path.basename(file)}: layout OK (nothing clipped at the print width)`);
      } else {
        bad++;
        console.error(`✗ ${path.basename(file)}: ${hits.length} element(s) overflow the print width — will clip:`);
        for (const h of hits) {
          const by = h.over > 0 ? `content +${h.over}px wider than its box` : `+${h.past}px past content edge`;
          console.error(`    <${h.tag}> ${by} :: ${h.text}`);
        }
      }
    }
  } finally {
    await browser.close();
  }
  process.exit(bad ? 1 : 0);
}

main();
