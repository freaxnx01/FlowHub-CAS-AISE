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
//   - mermaid-isomorphic — pre-render mermaid diagrams to inline SVG so
//     they survive Chromium's print pipeline.
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
  const args = { input: null, output: null, title: null };
  const rest = [];
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--title") {
      args.title = argv[++i];
    } else {
      rest.push(a);
    }
  }
  [args.input, args.output] = rest;
  if (!args.input || !args.output) {
    console.error("usage: render.mjs <input.md> <output.pdf> [--title 'Title']");
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
    linkify: true,
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

function wrapHtml({ title, body, css }) {
  const escapedTitle = (title ?? "").replace(/[<>&]/g, (c) =>
    c === "<" ? "&lt;" : c === ">" ? "&gt;" : "&amp;",
  );
  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>${escapedTitle}</title>
<style>
${css}
.markdown-body { box-sizing: border-box; max-width: 980px; margin: 0 auto; padding: 24px 36px; }
@media print {
  .markdown-body { max-width: 100%; padding: 0; }
  pre, code { font-size: 11px; }
  table { page-break-inside: avoid; }
  pre { page-break-inside: avoid; }
  h1, h2, h3, h4 { page-break-after: avoid; }
}
.mermaid svg { max-width: 100%; height: auto; }
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
  const { input, output, title } = parseArgs(process.argv.slice(2));
  const inputAbs = path.resolve(input);
  const outputAbs = path.resolve(output);

  const md = await fs.readFile(inputAbs, "utf8");
  const renderer = buildMarkdownIt();
  const html = renderer.render(md);

  const css = await loadCss();
  const fullHtml = wrapHtml({ title: title ?? path.basename(input, ".md"), body: html, css });

  const browser = await puppeteer.launch({
    headless: true,
    args: ["--no-sandbox", "--disable-dev-shm-usage"],
  });
  try {
    const page = await browser.newPage();
    await page.setContent(fullHtml, { waitUntil: "networkidle0" });
    // Wait for client-side mermaid render to finish (or skip after 15s).
    try {
      await page.waitForFunction("window.__mermaidDone === true", { timeout: 15_000 });
    } catch {
      console.error("mermaid render timed out — proceeding with whatever is rendered");
    }
    await page.emulateMediaType("print");
    await page.pdf({
      path: outputAbs,
      format: "A4",
      margin: { top: "20mm", right: "18mm", bottom: "22mm", left: "18mm" },
      printBackground: true,
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
