Ich erstelle dir alle 4 Ansätze als eine kommentierte, präsentierbare HTML-Datei — perfekt um an der PVA vorgestellt zu werden.Hier ist deine fertige `master-detail.html` — eine einzige Datei, die alle 4 Ansätze in Tabs demonstriert, mit echten Mock-Daten (Schweizer Personen) und vollem CRUD.

**Was implementiert ist:**

| Tab | Ansatz | Technik |
|-----|--------|---------|
| ① | HTML-Form (Full Reload) | `<form>` POST → simulierter Server-Redirect, ganze Seite neu |
| ② | Ajax Partial Update | `XMLHttpRequest`-Simulation → Server gibt HTML-Fragment zurück, nur `#master-region` wird ersetzt |
| ③ | fetch() / JSON-API | `fetch('/api/persons/{id}')` → JSON → JavaScript befüllt Formularfelder |
| ④ | JavaScript State | Reaktives Render-Pattern (Grundprinzip React/Vue), kein Server |

**Jeder Ansatz hat:**
- Ein farblich markiertes Erklärungsbanner mit dem Kernkonzept
- `// code-note`-Kommentare die zeigen, wie das echte Pendant (mit echtem Server) aussieht
- Toast-Benachrichtigungen die anzeigen, welcher Mechanismus gerade ausgelöst wurde
- CSS Grid Layout wie im Mockup (Master oben, Detail unten)
- Vollständiges CRUD (Auswählen, Bearbeiten, Speichern, Abbrechen, Löschen)

Einfach die Datei im Browser öffnen — kein Server nötig.
