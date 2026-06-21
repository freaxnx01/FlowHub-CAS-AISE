#!/usr/bin/env python3
"""Outline-preserving PDF merge for the Moodle upload set.

Concatenates a page range of the slide deck (landscape) in front of a text
document (portrait) into one mixed-orientation PDF, keeping a navigable outline:
a top-level bookmark for the deck part and one for the document, each carrying the
source's own outline. `pdfunite` cannot do this (it drops outlines), so the
package-submission recipe uses this instead.

Usage:
  merge.py <out.pdf> <deck.pdf> <start> <end> <doc.pdf> "<DeckLabel>" "<DocLabel>"
  (start/end are 1-based, inclusive, into the deck)
"""

import sys
from pypdf import PdfWriter

out_path, deck_path, start_s, end_s, doc_path, deck_label, doc_label = sys.argv[1:8]
start, end = int(start_s), int(end_s)

writer = PdfWriter()
# Deck pages [start..end] (1-based) → pypdf pages tuple is (start0, stop_exclusive)
writer.append(deck_path, pages=(start - 1, end), outline_item=deck_label)
# Full text document, its heading outline nested under doc_label
writer.append(doc_path, outline_item=doc_label)

with open(out_path, "wb") as fh:
    writer.write(fh)

print(
    f"wrote {out_path} ({len(writer.pages)} pages, outline: '{deck_label}' + '{doc_label}')"
)
