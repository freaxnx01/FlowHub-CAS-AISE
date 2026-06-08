# FlowHub — CAS-Präsentation

8–10-Minuten-Präsentation für die CAS-AISE-Schlusspräsentation:
das Produkt **und** die Erfahrung, es mit KI zu bauen.

| Datei | Inhalt |
|---|---|
| `flowhub-praesentation.md` | Quelle (Marp-Markdown). Sprechnotizen stehen als HTML-Kommentare pro Folie. |
| `theme/flowhub.css` | Marp-Theme, Farbpalette an das Architektur-Schaubild angelehnt. |
| `flowhub-praesentation.pdf` | Export inkl. Sprechnotizen (`--pdf-notes`). |
| `flowhub-praesentation.html` | Eigenständiges HTML-Deck (im Browser präsentierbar, `F` = Vollbild). |

## Rendern

Voraussetzung: Node + Chromium (für PDF/PNG). Aus diesem Ordner:

```bash
# HTML (kein Browser nötig)
npx @marp-team/marp-cli --html \
  --theme-set theme/flowhub.css --allow-local-files \
  flowhub-praesentation.md -o flowhub-praesentation.html

# PDF mit Sprechnotizen
npx @marp-team/marp-cli --html --pdf --pdf-notes \
  --theme-set theme/flowhub.css --allow-local-files \
  flowhub-praesentation.md -o flowhub-praesentation.pdf

# Folien als Einzelbilder (Vorschau/Verifikation)
npx @marp-team/marp-cli --html --images png \
  --theme-set theme/flowhub.css --allow-local-files \
  flowhub-praesentation.md -o slide.png
```

## Präsentieren

- **Live aus dem Browser:** `flowhub-praesentation.html` öffnen, `F` für Vollbild.
  Presenter-View mit Sprechnotizen:
  `npx @marp-team/marp-cli --html -p flowhub-praesentation.md` (Preview-Server).
- **Offline:** das PDF; die Sprechnotizen liegen als PDF-Seitennotizen bei.

> **Wichtig:** Das Deck nutzt rohes HTML (`<div class="cols">` für die Zwei-Spalten-
> Folie, `<span class="small">`). Das rendert nur mit dem **`--html`**-Flag — ohne
> das Flag fällt die Zwei-Spalten-Folie zu einer einspaltigen Liste zusammen.
> In der **VS-Code-*Marp*-Extension** ist rohes HTML standardmässig deaktiviert:
> Setting `markdown.marp.enableHtml` aktivieren, sonst bricht dieselbe Folie.

## Bekannter Export-Hinweis (Emoji im Architektur-SVG)

Die Architektur-Folie bindet das wiederverwendete
`../projektbeschreibung/FlowHub_Architecture-v2.svg` ein. Dessen Emoji-Glyphen
(📱 ⚙️ 🎯 …) rendern im Browser mit Emoji-Schrift korrekt, im **Headless-Chromium-
Export** (PDF/PNG) aber als Platzhalter-Kästchen (□), wenn keine Emoji-Schrift
installiert ist. Fix bei Bedarf:

```bash
sudo apt-get install -y fonts-noto-color-emoji   # danach neu rendern
```
