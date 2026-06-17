#!/usr/bin/env node
// Headless Markdown -> PDF, mirroring the VS Code "Markdown PDF" extension
// (yzane.markdown-pdf) so PDFs are visually consistent whether they're
// regenerated from CI, this CLI, or the extension itself.
//
// Toolchain:
//   - markdown-it (+ anchor / emoji / task-lists) — same renderer as VS Code's
//     built-in markdown preview that the extension wraps.
//   - github-markdown-css — standard GitHub markdown stylesheet (light mode).
//   - highlight.js — syntax highlighting (extension's default).
//   - mermaid — diagrams render client-side in the headless browser; the
//     package is vendored locally and served via request interception, so
//     rendering works offline and survives Chromium's print pipeline.
//   - puppeteer — headless Chromium, identical mechanism the extension uses.
//
// Usage:
//   node render.mjs <input.md> <output.pdf> [--title "Doc title"]

import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import puppeteer from "puppeteer";
import MarkdownIt from "markdown-it";
import anchor from "markdown-it-anchor";
import { full as emoji } from "markdown-it-emoji";
import taskLists from "markdown-it-task-lists";
import hljs from "highlight.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function parseArgs(argv) {
  const args = { input: null, output: null, title: null, compact: false };
  const rest = [];
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--title") {
      args.title = argv[++i];
    } else if (a === "--compact") {
      args.compact = true;
    } else {
      rest.push(a);
    }
  }
  [args.input, args.output] = rest;
  if (!args.input || !args.output) {
    console.error("usage: render.mjs <input.md> <output.pdf> [--title 'Title'] [--compact]");
    process.exit(64);
  }
  return args;
}

async function loadCss() {
  const githubCss = await fs.readFile(
    path.join(__dirname, "node_modules/github-markdown-css/github-markdown-light.css"),
    "utf8",
  );
  const hljsCss = await fs.readFile(
    path.join(__dirname, "node_modules/highlight.js/styles/github.css"),
    "utf8",
  );
  return githubCss + "\n" + hljsCss;
}

function buildMarkdownIt() {
  const md = new MarkdownIt({
    html: true,
    // linkify off: it auto-linked domain-like text such as "ASP.NET" / ".NET".
    // All real links in these docs are explicit ([text](url) or <url>).
    linkify: false,
    typographer: false,
    highlight: (code, lang) => {
      if (lang && hljs.getLanguage(lang)) {
        try {
          return (
            '<pre><code class="hljs language-' +
            lang +
            '">' +
            hljs.highlight(code, { language: lang, ignoreIllegals: true }).value +
            "</code></pre>"
          );
        } catch {}
      }
      return '<pre><code class="hljs">' + md.utils.escapeHtml(code) + "</code></pre>";
    },
  })
    .use(anchor, { permalink: false })
    .use(emoji)
    .use(taskLists, { enabled: false });

  // Rewrite ```mermaid fences from <pre><code class="hljs language-mermaid">…</code></pre>
  // to <div class="mermaid">…</div> so the client-side mermaid.js (loaded from CDN
  // in wrapHtml) renders them to SVG before puppeteer prints. This matches the
  // VS Code extension's behaviour, which also defers mermaid rendering to a
  // browser context rather than pre-rendering server-side.
  md.renderer.rules.fence = (tokens, idx) => {
    const token = tokens[idx];
    if (token.info?.trim() === "mermaid") {
      return `<div class="mermaid">${md.utils.escapeHtml(token.content)}</div>\n`;
    }
    const langClass = token.info ? ` language-${token.info.trim()}` : "";
    const highlighted =
      token.info && hljs.getLanguage(token.info.trim())
        ? hljs.highlight(token.content, { language: token.info.trim(), ignoreIllegals: true }).value
        : md.utils.escapeHtml(token.content);
    return `<pre><code class="hljs${langClass}">${highlighted}</code></pre>\n`;
  };

  return md;
}

function wrapHtml({ title, body, css, compact }) {
  const escapedTitle = (title ?? "").replace(/[<>&]/g, (c) =>
    c === "<" ? "&lt;" : c === ">" ? "&gt;" : "&amp;",
  );
  const compactCss = compact
    ? `
.markdown-body { font-size: 12px; line-height: 1.45; }
.markdown-body h1 { font-size: 1.6em; margin: 0.4em 0 0.4em; padding-bottom: 0.2em; }
.markdown-body h2 { font-size: 1.25em; margin: 0.8em 0 0.3em; padding-bottom: 0.15em; }
.markdown-body h3 { font-size: 1.1em; margin: 0.6em 0 0.25em; }
.markdown-body p, .markdown-body ul, .markdown-body ol { margin: 0.35em 0; }
.markdown-body li { margin: 0.15em 0; }
.markdown-body table { font-size: 11px; }
.markdown-body table th, .markdown-body table td { padding: 4px 8px; }
.markdown-body hr { margin: 0.6em 0; }
`
    : "";
  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>${escapedTitle}</title>
<style>
${css}
.markdown-body { box-sizing: border-box; max-width: 980px; margin: 0 auto; padding: 24px 36px; }
/* github-markdown-css renders tables as display:block; width:max-content; overflow:auto,
   which CLIPS wide tables in print (no horizontal scroll on paper). Force full-width
   wrapping tables so cell content wraps instead of being cut off. */
.markdown-body table { display: table; width: 100%; table-layout: auto; }
.markdown-body th, .markdown-body td { overflow-wrap: anywhere; }
@media print {
  .markdown-body { max-width: 100%; padding: 0; }
  pre, code { font-size: 11px; }
  /* Let long tables flow across page breaks (avoids near-empty pages when a
     big table would otherwise be pushed whole onto the next page); repeat the
     header row on each page. */
  table, tr, td, th { page-break-inside: auto; }
  thead { display: table-header-group; }
  pre { page-break-inside: avoid; }
  h1, h2, h3, h4 { page-break-after: avoid; }
}
.mermaid svg { max-width: 100%; height: auto; }
${compactCss}
</style>
</head>
<body class="markdown-body">
${body}
<script type="module">
  import mermaid from "https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs";
  mermaid.initialize({ startOnLoad: false, theme: "default", securityLevel: "loose" });
  (async () => {
    try { await mermaid.run({ querySelector: ".mermaid" }); }
    catch (e) { console.error("mermaid render failed", e); }
    finally { window.__mermaidDone = true; }
  })();
</script>
</body>
</html>`;
}

async function main() {
  const { input, output, title, compact } = parseArgs(process.argv.slice(2));
  const inputAbs = path.resolve(input);
  const outputAbs = path.resolve(output);

  const md = await fs.readFile(inputAbs, "utf8");
  const renderer = buildMarkdownIt();
  const html = renderer.render(md);

  const css = await loadCss();
  const fullHtml = wrapHtml({ title: title ?? path.basename(input, ".md"), body: html, css, compact });

  const browser = await puppeteer.launch({
    headless: true,
    args: ["--no-sandbox", "--disable-dev-shm-usage"],
  });
  try {
    const page = await browser.newPage();
    // Serve mermaid.js (and its lazily-loaded diagram chunks) from the locally
    // vendored package instead of the CDN, so diagram rendering works fully
    // offline and can't be broken by CDN/egress issues at build time. The page
    // still `import`s the jsdelivr URL (see wrapHtml); we intercept and fulfil it
    // from node_modules/mermaid/dist, which keeps the relative chunk imports
    // resolving to the same intercepted base.
    const MERMAID_CDN = "https://cdn.jsdelivr.net/npm/mermaid@11/dist/";
    const mermaidDist = path.join(__dirname, "node_modules", "mermaid", "dist");
    await page.setRequestInterception(true);
    page.on("request", async (req) => {
      const url = req.url();
      if (url.startsWith(MERMAID_CDN)) {
        try {
          const rel = url.slice(MERMAID_CDN.length).split("?")[0];
          const body = await fs.readFile(path.join(mermaidDist, rel));
          // The setContent page has a `null` origin, so the cross-origin module
          // fetch needs CORS headers (the real jsdelivr sends `*`).
          await req.respond({
            status: 200,
            contentType: "text/javascript",
            headers: { "Access-Control-Allow-Origin": "*" },
            body,
          });
        } catch (e) {
          console.error(`failed to serve vendored mermaid asset (${url}): ${e.message}`);
          await req.abort();
        }
        return;
      }
      await req.continue();
    });
    await page.setContent(fullHtml, { waitUntil: "networkidle0" });
    // Wait for client-side mermaid render to finish (rendering is offline via the
    // vendored package, so this is fast; the timeout only bounds a genuine hang).
    try {
      await page.waitForFunction("window.__mermaidDone === true", { timeout: 30_000 });
    } catch {
      console.error("mermaid render timed out — proceeding with whatever is rendered");
    }
    // Guard: every ```mermaid``` block must have rendered to an <svg>. A CDN
    // failure (mermaid.js is fetched at build time), a render timeout, or a
    // diagram syntax error leaves the raw source in the <div class="mermaid">.
    // Fail loudly rather than silently shipping a PDF with broken diagrams —
    // this PDF is the self-contained submission artefact.
    const mermaidFailures = await page.evaluate(() =>
      Array.from(document.querySelectorAll("div.mermaid"))
        .map((el, i) => {
          const svg = el.querySelector("svg");
          const ok = !!svg && !/syntax error/i.test(svg.textContent || "");
          return { i, ok, snippet: (el.textContent || "").trim().slice(0, 60) };
        })
        .filter((b) => !b.ok),
    );
    if (mermaidFailures.length > 0) {
      console.error(
        `mermaid render guard: ${mermaidFailures.length} diagram(s) did not render ` +
          "(CDN unreachable, timeout, or syntax error):",
      );
      for (const f of mermaidFailures) {
        console.error(`  - block #${f.i}: ${f.snippet}…`);
      }
      throw new Error(
        "refusing to write a PDF with unrendered mermaid diagrams — " +
          "rebuild with network access to cdn.jsdelivr.net, or fix the diagram syntax",
      );
    }
    await page.emulateMediaType("print");
    const printMargin = compact
      ? { top: "12mm", right: "14mm", bottom: "14mm", left: "14mm" }
      : { top: "20mm", right: "18mm", bottom: "22mm", left: "18mm" };
    await page.pdf({
      path: outputAbs,
      format: "A4",
      margin: printMargin,
      printBackground: true,
      // Generate a navigable bookmark outline from the heading structure (h1–h6)
      // so large documents like the submission bundle (~280pp) are usable in any
      // PDF reader's sidebar, offline. `outline` is derived from the tagged-PDF
      // tag tree, so `tagged` must be enabled too.
      tagged: true,
      outline: true,
      displayHeaderFooter: true,
      headerTemplate: "<span></span>",
      footerTemplate:
        '<div style="font-size:9px; width:100%; padding:0 18mm; color:#666; display:flex; justify-content:space-between;">' +
        '<span>' +
        (title ?? path.basename(input, ".md")) +
        '</span>' +
        '<span><span class="pageNumber"></span> / <span class="totalPages"></span></span>' +
        '</div>',
    });
  } finally {
    await browser.close();
  }
  console.error(`wrote ${outputAbs}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
