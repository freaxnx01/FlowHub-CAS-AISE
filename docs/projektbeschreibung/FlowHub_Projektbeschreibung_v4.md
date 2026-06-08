# FlowHub – Projektbeschreibung

**CAS AI-Assisted Software Engineering (AISE)**
W4B-C-AS001 · ZH-Sa-1 · FS26

**Student:** Andreas Imboden
**Datum:** Februar 2026

---

## 1. Vision

FlowHub ist ein KI-gestützter persönlicher Eingangskorb, der Informationsschnipsel aus dem Alltag automatisch erkennt, kategorisiert und an die richtigen Services weiterleitet.

Der digitale Alltag produziert ständig kleine Informationsfragmente: ein Film den man schauen möchte, ein Tech-Artikel zum Lesen, ein Foto eines Kassenbelegs. Heute landen diese Schnipsel verstreut in verschiedenen Apps, Chats oder werden schlicht vergessen.

FlowHub schafft einen einzigen Eingang für all diese Inputs und erledigt die Ablage automatisch – mit KI-Unterstützung und minimalem Aufwand für den Benutzer.

---

## 2. Stakeholder

**Primär: Homelab-Betreiber (Persona: Andreas)**

- Technisch versierter Anwender
- Betreibt Self-Hosted Services im Heimnetzwerk (Proxmox, Docker)
- Nutzt bereits: paperless-ngx, Passbolt, GitLab, Vikunja, Wallabag
- Problem: Informationsschnipsel landen überall, kein einheitlicher Eingang
- Ziel: Schnelle, reibungslose Erfassung von Alltagsinformationen via Telegram

**Sekundär: Digital Hoarders**

- Sammeln Artikel, Bookmarks, Notizen in vielen Tools
- Frustriert durch manuelles Kategorisieren und Sortieren
- Würden von automatisierter Ablage profitieren

**Tertiär: CAS-Dozenten (FFHS)**

- Erwarten: Verteilte Web-Applikation mit KI-Einsatz
- Erwarten: Saubere Software-Architektur-Dokumentation
- Erwarten: Reflexion über KI-unterstützte Entwicklung

---

## 3. Kundenbedürfnis

Das adressierte Kernbedürfnis ist **"Capture without friction"**: Der Benutzer möchte eine Information festhalten, ohne in diesem Moment entscheiden zu müssen, wohin sie gehört und wie sie abgelegt wird.

**Heute:**

> Idee → Welche App? → App öffnen → Kategorisieren → Ablegen
> = 5+ Schritte, Kontextwechsel, oft vergessen

**Mit FlowHub:**

> Idee → Telegram Bot → Fertig
> = 1 Schritt, KI übernimmt den Rest

---

## 4. Externe Services

FlowHub integriert ausschliesslich Self-Hosted Services aus dem eigenen Homelab (kein Cloud-SaaS):

| Service | URL | Zweck |
|---|---|---|
| Vikunja | https://vikunja.io/ | Task-Management für Bücher, Filme/TV |
| paperless-ngx | https://docs.paperless-ngx.com/ | Dokumenten-Management-System (DMS) |
| Wallabag | https://wallabag.org/ | Read-Later Service für Artikel |
| GitLab (Self-Hosted) | https://gitlab.freaxnx01.ch/ | Repository mit Obsidian Markdown Files (Knowledge Base) |

---

## 5. Funktionsübersicht

### 5.1 Skill-System

FlowHub verwendet ein **Skill-basiertes Routing**: Jeder eingehende Input wird einem Skill zugewiesen, der die Ablage in den passenden Ziel-Service übernimmt. Die Erkennung erfolgt über Keywords, URL-Muster und – falls nötig – ein lokales LLM.

#### ArticleSkill – Artikel zum späteren Lesen (→ Wallabag)

Erkennung: URL von Nachrichtenportalen, Blogs, Fachzeitschriften

Beispiele:
- `https://www.heise.de/select/ct/2026/4/2533109542020998570`
- `https://www.heise.de/ratgeber/Fluechtige-SSH-Schluessel-mit-opkssh-und-OpenID-Connect-generieren-10639864.html`

---

#### HomelabSkill – Homelab-Services zum Ausprobieren (→ Vikunja)

Erkennung: URL von Software/Service-Websites; Keywords: homelab, ausprobieren, install, self-host, try

Beispiele:
- `https://adguard.com/de/welcome.html`
- `https://jellyfin.org/`

---

#### BookSkill – Bücher & IT-Bücher (→ Vikunja)

Erkennung: URL von Buchshops (exlibris, galaxus, amazon, orellfuessli); Keywords: buch, lesen, bestellen, isbn

Beispiele:
- `https://www.exlibris.ch/de/buecher-buch/english-books/linus-torvalds/just-for-fun/id/9780066620732/`
- `https://www.galaxus.ch/de/s18/product/eine-kurze-geschichte-der-menschheit-deutsch-yuval-noah-harari-2015-sachbuecher-7003551`

---

#### MovieSkill – Filme & TV-Serien (→ Vikunja)

Erkennung: Keywords: schauen, watch, film, movie, serie, TV; Google-Share-URLs (`share.google/...`)

Beispiele:
- `The Imitation Game – Ein streng geheimes Leben` + `https://share.google/1vwEtMUiRriic4nNi`
- `Star Trek` + `https://share.google/338RxCPCv9Ytm0wgA`

---

#### KnowledgeSkill – Knowledge Base Einträge (→ Obsidian via GitLab)

Erkennung: Artikel-URL mit explizitem Wissensbasis-Kontext; Keywords: notiz, wissen, merken, knowledge, speichern

Beispiele:
- `https://www.heise.de/select/ct/2026/5/2532311091092661684`
- `https://www.heise.de/select/ct/2026/5/2534615175182620957`

---

#### DocumentSkill – Dokumente (→ paperless-ngx)

Erkennung: Dateianhang (PDF, Foto); Keywords: quittung, rechnung, beleg, dokument

Beispiele:
- Foto einer Quittung
- PDF einer Rechnung

---

#### QuoteSkill – Zitate mit KI-Anreicherung (→ Obsidian via GitLab)

Erkennung: Keywords: zitat, quote, gesagt von; Anführungszeichen im Text; Share-Links auf Zitate

Enrichment via KI: Autor und AuthorInfo (Kontext, Werk, Biografie-Snippet) werden automatisch ergänzt

Beispiel:
- Shannons Zitat zur Informationstheorie: `https://g.co/gemini/share/917675e7a359`

---

#### GenericSkill – Fallback (→ FlowHub Inbox / PostgreSQL)

Greift wenn kein anderer Skill passt. Input landet in der Inbox zur späteren manuellen Klassifizierung.

---

#### Skill-Vorschlag bei unbekanntem Input

Wenn die Kategorisierung keinen bestehenden Skill zuweisen kann, schlägt das System dem Benutzer einen neuen Skill vor:

```
Bot: "Ich konnte diesen Input keinem Skill zuweisen.
      Möchtest du einen neuen Skill erstellen?
      [Ja, Skill erstellen] [In Inbox ablegen] [Verwerfen]"
```

Bei Bestätigung wird eine SKILL.md-Vorlage generiert und der Input zwischengespeichert. Der neue Skill kann anschliessend konfiguriert und aktiviert werden.

---

### 5.2 MVP – CAS Projektarbeit

Der MVP-Scope ist bewusst eng gefasst. Komplexität wird durch saubere Architektur beherrscht, nicht durch Featureumfang.

**Input-Kanal**

| Feature | Beschreibung | Status |
|---|---|---|
| Telegram Bot (Text) | Textnachrichten empfangen und verarbeiten | ✅ MVP |
| Telegram Bot (Datei/Foto) | Dateianhänge empfangen und verarbeiten | ✅ MVP |

**Skill-System (automatische Erkennung und Routing)**

| Skill | Erkennung | Ziel-Service | Status |
|---|---|---|---|
| ArticleSkill | URL (News/Blog/Fachmagazin) | Wallabag | ✅ MVP |
| HomelabSkill | URL (Software/Service) + Keywords | Vikunja | ✅ MVP |
| BookSkill | URL (Buchshop) + Keywords | Vikunja | ✅ MVP |
| MovieSkill | Keywords + Google-Share-URL | Vikunja | ✅ MVP |
| DocumentSkill | Dateianhang (Foto/PDF) | paperless-ngx | ✅ MVP |
| KnowledgeSkill | URL + Keywords | Obsidian (GitLab) | Future |
| QuoteSkill | Keywords + Anführungszeichen | Obsidian (GitLab) | Future |
| GenericSkill | Kein anderer Skill greift | FlowHub Inbox (PostgreSQL) | ✅ MVP |
| Skill-Vorschlag | Unbekannter Input → neuen Skill vorschlagen | – | Future |

**Ziel-Services (Integrationen)**

| Service | Typ | Integration | MVP-Umfang |
|---|---|---|---|
| Wallabag | Self-Hosted | REST API | URL speichern |
| Vikunja | Self-Hosted | REST API | Task/Item erstellen |
| paperless-ngx | Self-Hosted | REST API | Dokument hochladen |
| Obsidian / GitLab | Self-Hosted | Git API | Markdown-Datei erstellen |
| FlowHub Inbox | Eigene DB | PostgreSQL | Fallback-Speicher |

**Benutzerinteraktion**

| Feature | Beschreibung | Status |
|---|---|---|
| Direkte Verarbeitung | Klarer Input → sofortige Aktion | ✅ MVP |
| Rückfrage mit Auswahl | Unklarer Input → Bot fragt mit 2–3 Optionen zurück | ✅ MVP |
| Bestätigungs-Feedback | Bot antwortet mit Ergebnis | ✅ MVP |
| Skill-Vorschlag | Unbekannter Input → neuen Skill vorschlagen | Future |

**KI-Integration**

| Feature | Beschreibung | Status |
|---|---|---|
| Keyword-basierte Erkennung | Zuverlässiger Fallback ohne KI-Kosten | ✅ MVP |
| Microsoft.Extensions.AI + Ollama | Lokales LLM für Skill-Erkennung | ✅ MVP (wenn Zeit reicht) |
| Confidence-Score | Schwellwert bestimmt ob Rückfrage nötig | ✅ MVP (wenn Zeit reicht) |
| Enrichment (QuoteSkill) | Autor, AuthorInfo via KI ergänzen | Future |

### 5.3 Future – Spätere Versionen (nach CAS)

Diese Features sind architekturell vorbereitet, werden aber nicht für die Abgabe implementiert. Sie belegen die Zukunftsfähigkeit der Architektur.

**Erweiterte Skill-Funktionalität**

| Skill | Erweiterung |
|---|---|
| MovieSkill | IMDB/OMDB Metadaten (Titel, Rating, Jahr, Genre) |
| ArticleSkill | Zusätzlich: Obsidian-Notiz mit KI-Summary erstellen |
| DocumentSkill | AI OCR-Analyse (Betrag, Datum, Händler, Tags) |
| QuoteSkill | Vollständige Enrichment-Pipeline: Autor, Werk, Kontext |
| KnowledgeSkill | RAG-Integration: Obsidian als Wissensbasis für den Assistenten |
| Skill-Vorschlag | Automatische SKILL.md-Generierung via KI |

**Erweiterte Integrationen**

| Service | Erweiterung |
|---|---|
| Obsidian / GitLab | RAG-Quelle für den Assistenten |
| paperless-ngx | AI-gestützte Tagging und Metadaten-Extraktion |
| Vikunja | Erweiterte Metadaten (Cover, Rating, Genre für Filme/Bücher) |

**Erweiterte KI-Features**

| Feature | Beschreibung |
|---|---|
| Lerneffekt | Nutzerverhalten verbessert Confidence über Zeit |
| RAG | Obsidian als Wissensbasis für den Assistenten |
| Enrichment | Automatische Anreicherung von Quotes, Büchern, Filmen |

**Weitere Input-Kanäle**

| Kanal | Beschreibung |
|---|---|
| Email | Emails als Input verarbeiten |
| Web Upload | Browser-Extension oder Web-UI für Uploads |
| API | Direkte REST-API für Drittanwendungen |

**Deployment-Evolution**

| Phase | Beschreibung |
|---|---|
| Phase 1 (MVP) | Docker Compose auf Proxmox Homelab |
| Phase 2 (Future) | Migration zu k3s (Kubernetes) |

---

## 6. Systemarchitektur

### 6.1 Überblick

> **Hinweis — Konzept-/Zielarchitektur, nicht Abgabestand.** Das folgende
> Übersichtsbild zeigt die *angestrebte* Gesamtarchitektur und enthält bewusst
> auch noch **nicht implementierte** Bausteine (z. B. Ollama-LLM, Telegram-Kanal,
> Redis). Die tatsächlich gebaute, als-built-Architektur ist der **Modular
> Monolith** aus sechs Projekten mit MassTransit-Pipeline — maßgeblich sind dafür
> die C4-/Hexagonal-Diagramme in `docs/design/perspectives.md`, das
> ER-Diagramm in `docs/design/db/er.md` und die ADRs (insb. ADR 0002/0003/0005,
> jeweils mit „As built"-Notiz). Siehe auch `docs/spec/system-context.md`
> → „Current state (Block 5)".

<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 980" font-family="Segoe UI, Arial, sans-serif">
  <defs>
    <!-- Arrow marker -->
    <marker id="arrow-teal" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#2ECC71"/>
    </marker>
    <marker id="arrow-blue" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#3498DB"/>
    </marker>
    <marker id="arrow-dark" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#555"/>
    </marker>
    <!-- Drop shadow -->
    <filter id="shadow" x="-5%" y="-5%" width="110%" height="115%">
      <feDropShadow dx="0" dy="2" stdDeviation="3" flood-color="#00000022"/>
    </filter>
    <!-- Subtle shadow for inner boxes -->
    <filter id="shadow-sm" x="-5%" y="-5%" width="110%" height="115%">
      <feDropShadow dx="0" dy="1" stdDeviation="2" flood-color="#00000018"/>
    </filter>
  </defs>

  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- BACKGROUND -->
  <!-- ═══════════════════════════════════════════════════════ -->
  <rect width="1200" height="980" fill="#F7F9FC"/>

  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- TITLE -->
  <!-- ═══════════════════════════════════════════════════════ -->
  <text x="600" y="48" text-anchor="middle" font-size="26" font-weight="700" fill="#1A2B40" letter-spacing="0.5">FlowHub Systemarchitektur</text>
  <text x="600" y="70" text-anchor="middle" font-size="13" fill="#7F8C8D">.NET 10 / C# / ASP.NET Core · Blazor SSR · Microsoft.Extensions.AI</text>

  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- 1. INPUT LAYER -->
  <!-- ═══════════════════════════════════════════════════════ -->
  <rect x="60" y="88" width="1080" height="90" rx="14" fill="#D6EAF8" stroke="#3498DB" stroke-width="2" filter="url(#shadow)"/>
  <text x="600" y="118" text-anchor="middle" font-size="15" font-weight="700" fill="#1A5276">&#x1F4F1;  INPUT-LAYER</text>
  <text x="600" y="141" text-anchor="middle" font-size="13" fill="#2471A3">Telegram Bot  ·  Text + Datei + Foto</text>
  <text x="600" y="161" text-anchor="middle" font-size="11.5" fill="#5D8AA8" font-style="italic">Einziger Eingang · "Capture without friction"</text>

  <!-- Arrow Input → Backend -->
  <line x1="600" y1="178" x2="600" y2="202" stroke="#3498DB" stroke-width="3" marker-end="url(#arrow-blue)"/>

  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- 2. BACKEND CONTAINER -->
  <!-- ═══════════════════════════════════════════════════════ -->
  <rect x="60" y="205" width="1080" height="460" rx="14" fill="#D5F5E3" stroke="#27AE60" stroke-width="2.5" filter="url(#shadow)"/>
  <text x="340" y="232" text-anchor="middle" font-size="15" font-weight="700" fill="#1E8449">&#x2699;&#xFE0F;  FLOWHUB BACKEND</text>
  <text x="340" y="250" text-anchor="middle" font-size="12" fill="#27AE60">.NET 10 / C# / ASP.NET Core</text>

  <!-- ── Telegram Bot Service box ── -->
  <rect x="80" y="265" width="170" height="80" rx="10" fill="#FFFFFF" stroke="#27AE60" stroke-width="1.5" filter="url(#shadow-sm)"/>
  <text x="165" y="293" text-anchor="middle" font-size="13" font-weight="600" fill="#1E8449">&#x1F916; Telegram Bot</text>
  <text x="165" y="312" text-anchor="middle" font-size="11.5" fill="#555">Service</text>
  <text x="165" y="329" text-anchor="middle" font-size="10.5" fill="#888">Webhook Handler</text>

  <!-- Arrow Telegram → Skill-System -->
  <line x1="250" y1="305" x2="278" y2="305" stroke="#27AE60" stroke-width="2.5" marker-end="url(#arrow-teal)"/>

  <!-- ── SKILL-SYSTEM box ── -->
  <rect x="280" y="258" width="530" height="392" rx="12" fill="#FEF9E7" stroke="#F39C12" stroke-width="2" filter="url(#shadow-sm)"/>
  <text x="545" y="283" text-anchor="middle" font-size="14" font-weight="700" fill="#B7770D">&#x1F3AF;  SKILL-SYSTEM</text>
  <text x="545" y="300" text-anchor="middle" font-size="11" fill="#CA8A04">SkillRegistry  ·  lädt SKILL.md Files  ·  Dependency Injection</text>

  <!-- Skills grid: 4 columns × 2 rows -->
  <!-- Row 1 -->
  <!-- ArticleSkill -->
  <rect x="298" y="312" width="118" height="64" rx="8" fill="#FFFFFF" stroke="#F39C12" stroke-width="1.5" filter="url(#shadow-sm)"/>
  <text x="357" y="334" text-anchor="middle" font-size="18">&#x1F4F0;</text>
  <text x="357" y="352" text-anchor="middle" font-size="11.5" font-weight="600" fill="#333">ArticleSkill</text>
  <text x="357" y="368" text-anchor="middle" font-size="10" fill="#888">→ Wallabag</text>

  <!-- HomelabSkill -->
  <rect x="426" y="312" width="118" height="64" rx="8" fill="#FFFFFF" stroke="#F39C12" stroke-width="1.5" filter="url(#shadow-sm)"/>
  <text x="485" y="334" text-anchor="middle" font-size="18">&#x1F3E0;</text>
  <text x="485" y="352" text-anchor="middle" font-size="11.5" font-weight="600" fill="#333">HomelabSkill</text>
  <text x="485" y="368" text-anchor="middle" font-size="10" fill="#888">→ Vikunja</text>

  <!-- BookSkill -->
  <rect x="554" y="312" width="118" height="64" rx="8" fill="#FFFFFF" stroke="#F39C12" stroke-width="1.5" filter="url(#shadow-sm)"/>
  <text x="613" y="334" text-anchor="middle" font-size="18">&#x1F4DA;</text>
  <text x="613" y="352" text-anchor="middle" font-size="11.5" font-weight="600" fill="#333">BookSkill</text>
  <text x="613" y="368" text-anchor="middle" font-size="10" fill="#888">→ Vikunja</text>

  <!-- MovieSkill -->
  <rect x="682" y="312" width="118" height="64" rx="8" fill="#FFFFFF" stroke="#F39C12" stroke-width="1.5" filter="url(#shadow-sm)"/>
  <text x="741" y="334" text-anchor="middle" font-size="18">&#x1F3AC;</text>
  <text x="741" y="352" text-anchor="middle" font-size="11.5" font-weight="600" fill="#333">MovieSkill</text>
  <text x="741" y="368" text-anchor="middle" font-size="10" fill="#888">→ Vikunja</text>

  <!-- Row 2 -->
  <!-- DocumentSkill -->
  <rect x="298" y="390" width="118" height="64" rx="8" fill="#FFFFFF" stroke="#F39C12" stroke-width="1.5" filter="url(#shadow-sm)"/>
  <text x="357" y="412" text-anchor="middle" font-size="18">&#x1F4C4;</text>
  <text x="357" y="430" text-anchor="middle" font-size="11.5" font-weight="600" fill="#333">DocumentSkill</text>
  <text x="357" y="446" text-anchor="middle" font-size="10" fill="#888">→ paperless-ngx</text>

  <!-- KnowledgeSkill -->
  <rect x="426" y="390" width="118" height="64" rx="8" fill="#FFFFFF" stroke="#F39C12" stroke-width="1.5" filter="url(#shadow-sm)"/>
  <text x="485" y="412" text-anchor="middle" font-size="18">&#x1F9E0;</text>
  <text x="485" y="430" text-anchor="middle" font-size="11.5" font-weight="600" fill="#333">KnowledgeSkill</text>
  <text x="485" y="446" text-anchor="middle" font-size="10" fill="#888">→ Obsidian/GitLab</text>

  <!-- QuoteSkill -->
  <rect x="554" y="390" width="118" height="64" rx="8" fill="#FFFFFF" stroke="#F39C12" stroke-width="1.5" filter="url(#shadow-sm)"/>
  <text x="613" y="412" text-anchor="middle" font-size="18">&#x1F4AC;</text>
  <text x="613" y="430" text-anchor="middle" font-size="11.5" font-weight="600" fill="#333">QuoteSkill</text>
  <text x="613" y="446" text-anchor="middle" font-size="10" fill="#888">→ Obsidian/GitLab</text>

  <!-- GenericSkill -->
  <rect x="682" y="390" width="118" height="64" rx="8" fill="#FFFFFF" stroke="#F39C12" stroke-width="1.5" filter="url(#shadow-sm)"/>
  <text x="741" y="412" text-anchor="middle" font-size="18">&#x1F4E5;</text>
  <text x="741" y="430" text-anchor="middle" font-size="11.5" font-weight="600" fill="#333">GenericSkill</text>
  <text x="741" y="446" text-anchor="middle" font-size="10" fill="#888">→ Inbox (PostgreSQL)</text>

  <!-- Skill-Vorschlag banner -->
  <rect x="298" y="468" width="502" height="36" rx="8" fill="#FFF3CD" stroke="#FFC107" stroke-width="1.5"/>
  <text x="549" y="482" text-anchor="middle" font-size="11" font-weight="600" fill="#856404">&#x1F4A1; Skill-Vorschlag</text>
  <text x="549" y="497" text-anchor="middle" font-size="10.5" fill="#856404">Unbekannter Input → neuen Skill vorschlagen · SKILL.md generieren</text>

  <!-- Confidence / SkillDispatcher label -->
  <rect x="298" y="514" width="502" height="28" rx="6" fill="#FEF3E2" stroke="#E67E22" stroke-width="1" stroke-dasharray="4,3"/>
  <text x="549" y="532" text-anchor="middle" font-size="10.5" fill="#784212">SkillDispatcher · Keyword-Match → AI Confidence-Score (0.0–1.0) → Direkt / Rückfrage</text>

  <!-- ── AI-LAYER box ── -->
  <rect x="832" y="258" width="190" height="150" rx="10" fill="#FCF3CF" stroke="#F1C40F" stroke-width="2" filter="url(#shadow-sm)"/>
  <text x="927" y="282" text-anchor="middle" font-size="13" font-weight="700" fill="#9A7D0A">&#x1F916;  AI-LAYER</text>
  <line x1="852" y1="290" x2="1002" y2="290" stroke="#F1C40F" stroke-width="1"/>
  <text x="927" y="310" text-anchor="middle" font-size="11.5" fill="#7D6608">M.E.AI Abstraction</text>
  <text x="927" y="328" text-anchor="middle" font-size="11" fill="#555">Ollama (lokal)</text>
  <text x="927" y="344" text-anchor="middle" font-size="11" fill="#555">Llama 3.x</text>
  <text x="927" y="360" text-anchor="middle" font-size="11" fill="#555">Anthropic API (Fallback)</text>
  <text x="927" y="376" text-anchor="middle" font-size="11" fill="#555">Confidence-Scoring</text>
  <text x="927" y="392" text-anchor="middle" font-size="10" fill="#888" font-style="italic">Provider via Config wechselbar</text>

  <!-- ── REST CLIENTS box ── -->
  <rect x="832" y="424" width="190" height="166" rx="10" fill="#FDEDEC" stroke="#E74C3C" stroke-width="2" filter="url(#shadow-sm)"/>
  <text x="927" y="448" text-anchor="middle" font-size="13" font-weight="700" fill="#922B21">&#x1F527;  REST CLIENTS</text>
  <text x="927" y="463" text-anchor="middle" font-size="10" fill="#C0392B" font-style="italic">via Refit (typsicher)</text>
  <line x1="852" y1="470" x2="1002" y2="470" stroke="#E74C3C" stroke-width="1"/>
  <text x="927" y="490" text-anchor="middle" font-size="11.5" fill="#555">WallabagClient</text>
  <text x="927" y="508" text-anchor="middle" font-size="11.5" fill="#555">VikunjaClient</text>
  <text x="927" y="526" text-anchor="middle" font-size="11.5" fill="#555">PaperlessClient</text>
  <text x="927" y="544" text-anchor="middle" font-size="11.5" fill="#555">GitLabClient</text>
  <text x="927" y="580" text-anchor="middle" font-size="10" fill="#888" font-style="italic">ISkillHandler → Port-Abstraktion</text>

  <!-- Bidirectional arrow Skills ↔ AI -->
  <line x1="810" y1="340" x2="832" y2="340" stroke="#F39C12" stroke-width="2" marker-end="url(#arrow-dark)"/>
  <line x1="832" y1="330" x2="810" y2="330" stroke="#F1C40F" stroke-width="2" marker-end="url(#arrow-dark)"/>

  <!-- Arrow Skills → REST Clients -->
  <line x1="810" y1="490" x2="832" y2="490" stroke="#E74C3C" stroke-width="2" marker-end="url(#arrow-dark)"/>

  <!-- ── Blazor SSR box ── -->
  <rect x="80" y="550" width="720" height="100" rx="10" fill="#E8F8F5" stroke="#1ABC9C" stroke-width="2" filter="url(#shadow-sm)"/>
  <text x="440" y="580" text-anchor="middle" font-size="14" font-weight="700" fill="#0E6655">&#x1F5A5;&#xFE0F;  Blazor SSR  ·  Admin-Dashboard</text>
  <text x="440" y="600" text-anchor="middle" font-size="11.5" fill="#1ABC9C">Server-Side Rendering  ·  .NET-native  ·  kein JavaScript-Framework</text>
  <text x="440" y="618" text-anchor="middle" font-size="11" fill="#7DCEA0" font-style="italic">Inbox-Verwaltung  ·  Skill-Konfiguration  ·  Verarbeitungs-Logs</text>
  <text x="440" y="638" text-anchor="middle" font-size="11" fill="#7DCEA0" font-style="italic">Skill-Vorschlag Review  ·  Pending Inputs</text>

  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- 3. ARROWS Backend → Bottom layers -->
  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- → Persistence -->
  <line x1="230" y1="665" x2="230" y2="700" stroke="#2ECC71" stroke-width="3" marker-end="url(#arrow-teal)"/>
  <!-- → Homelab -->
  <line x1="600" y1="665" x2="600" y2="700" stroke="#2ECC71" stroke-width="3" marker-end="url(#arrow-teal)"/>
  <!-- REST Clients → Homelab -->
  <line x1="927" y1="665" x2="927" y2="700" stroke="#2ECC71" stroke-width="3" marker-end="url(#arrow-teal)"/>

  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- 4. PERSISTENCE -->
  <!-- ═══════════════════════════════════════════════════════ -->
  <rect x="60" y="703" width="280" height="130" rx="14" fill="#E8DAEF" stroke="#8E44AD" stroke-width="2" filter="url(#shadow)"/>
  <text x="200" y="730" text-anchor="middle" font-size="14" font-weight="700" fill="#6C3483">&#x1F4BE;  PERSISTENCE</text>
  <line x1="85" y1="738" x2="315" y2="738" stroke="#8E44AD" stroke-width="1"/>
  <text x="200" y="757" text-anchor="middle" font-size="12" fill="#512E5F">PostgreSQL 16</text>
  <text x="200" y="774" text-anchor="middle" font-size="11" fill="#7D3C98" font-style="italic">FlowHub Inbox · EF Core · Migrations</text>
  <text x="200" y="795" text-anchor="middle" font-size="12" fill="#512E5F">Redis 7</text>
  <text x="200" y="812" text-anchor="middle" font-size="11" fill="#7D3C98" font-style="italic">Pending Inputs · Session-State</text>

  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- 5. HOMELAB -->
  <!-- ═══════════════════════════════════════════════════════ -->
  <rect x="380" y="703" width="660" height="130" rx="14" fill="#FDEBD0" stroke="#E67E22" stroke-width="2" filter="url(#shadow)"/>
  <text x="710" y="730" text-anchor="middle" font-size="14" font-weight="700" fill="#A04000">&#x1F3E1;  HOMELAB  ·  Proxmox Self-Hosted</text>
  <line x1="405" y1="738" x2="1015" y2="738" stroke="#E67E22" stroke-width="1"/>

  <!-- Homelab service badges -->
  <!-- Wallabag -->
  <rect x="400" y="748" width="115" height="72" rx="8" fill="#FFFFFF" stroke="#E67E22" stroke-width="1.5"/>
  <text x="457" y="769" text-anchor="middle" font-size="16">&#x1F516;</text>
  <text x="457" y="787" text-anchor="middle" font-size="11" font-weight="600" fill="#333">Wallabag</text>
  <text x="457" y="803" text-anchor="middle" font-size="10" fill="#888">Read-Later</text>
  <text x="457" y="816" text-anchor="middle" font-size="9.5" fill="#aaa">wallabag.org</text>

  <!-- Vikunja -->
  <rect x="648" y="748" width="115" height="72" rx="8" fill="#FFFFFF" stroke="#E67E22" stroke-width="1.5"/>
  <text x="705" y="769" text-anchor="middle" font-size="16">&#x2705;</text>
  <text x="705" y="787" text-anchor="middle" font-size="11" font-weight="600" fill="#333">Vikunja</text>
  <text x="705" y="803" text-anchor="middle" font-size="10" fill="#888">Task-Management</text>
  <text x="705" y="816" text-anchor="middle" font-size="9.5" fill="#aaa">vikunja.io</text>

  <!-- paperless-ngx -->
  <rect x="772" y="748" width="115" height="72" rx="8" fill="#FFFFFF" stroke="#E67E22" stroke-width="1.5"/>
  <text x="829" y="769" text-anchor="middle" font-size="16">&#x1F5C2;&#xFE0F;</text>
  <text x="829" y="787" text-anchor="middle" font-size="11" font-weight="600" fill="#333">paperless-ngx</text>
  <text x="829" y="803" text-anchor="middle" font-size="10" fill="#888">DMS</text>
  <text x="829" y="816" text-anchor="middle" font-size="9.5" fill="#aaa">paperless-ngx.com</text>

  <!-- GitLab + Obsidian -->
  <rect x="896" y="748" width="130" height="72" rx="8" fill="#FFFFFF" stroke="#E67E22" stroke-width="1.5"/>
  <text x="961" y="769" text-anchor="middle" font-size="16">&#x1F4D3;</text>
  <text x="961" y="787" text-anchor="middle" font-size="11" font-weight="600" fill="#333">GitLab + Obsidian</text>
  <text x="961" y="803" text-anchor="middle" font-size="10" fill="#888">Knowledge Base</text>
  <text x="961" y="816" text-anchor="middle" font-size="9.5" fill="#aaa">gitlab.freaxnx01.ch</text>

  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- LEGEND -->
  <!-- ═══════════════════════════════════════════════════════ -->
  <text x="60" y="862" font-size="12" font-weight="600" fill="#555">Legende:</text>

  <!-- Input Layer -->
  <rect x="60" y="872" width="18" height="18" rx="4" fill="#D6EAF8" stroke="#3498DB" stroke-width="1.5"/>
  <text x="85" y="885" font-size="11.5" fill="#444">Input Layer</text>

  <!-- Backend -->
  <rect x="175" y="872" width="18" height="18" rx="4" fill="#D5F5E3" stroke="#27AE60" stroke-width="1.5"/>
  <text x="200" y="885" font-size="11.5" fill="#444">Backend</text>

  <!-- Skills -->
  <rect x="280" y="872" width="18" height="18" rx="4" fill="#FEF9E7" stroke="#F39C12" stroke-width="1.5"/>
  <text x="305" y="885" font-size="11.5" fill="#444">Skills</text>

  <!-- AI -->
  <rect x="370" y="872" width="18" height="18" rx="4" fill="#FCF3CF" stroke="#F1C40F" stroke-width="1.5"/>
  <text x="395" y="885" font-size="11.5" fill="#444">AI-Layer</text>

  <!-- REST Clients -->
  <rect x="470" y="872" width="18" height="18" rx="4" fill="#FDEDEC" stroke="#E74C3C" stroke-width="1.5"/>
  <text x="495" y="885" font-size="11.5" fill="#444">REST Clients</text>

  <!-- Blazor -->
  <rect x="590" y="872" width="18" height="18" rx="4" fill="#E8F8F5" stroke="#1ABC9C" stroke-width="1.5"/>
  <text x="615" y="885" font-size="11.5" fill="#444">Blazor SSR</text>

  <!-- Persistence -->
  <rect x="700" y="872" width="18" height="18" rx="4" fill="#E8DAEF" stroke="#8E44AD" stroke-width="1.5"/>
  <text x="725" y="885" font-size="11.5" fill="#444">Persistence</text>

  <!-- Homelab -->
  <rect x="820" y="872" width="18" height="18" rx="4" fill="#FDEBD0" stroke="#E67E22" stroke-width="1.5"/>
  <text x="845" y="885" font-size="11.5" fill="#444">Homelab (Self-Hosted)</text>

  <!-- ═══════════════════════════════════════════════════════ -->
  <!-- FOOTER -->
  <!-- ═══════════════════════════════════════════════════════ -->
  <line x1="60" y1="908" x2="1140" y2="908" stroke="#BDC3C7" stroke-width="1"/>
  <text x="600" y="928" text-anchor="middle" font-size="11" fill="#95A5A6">FlowHub – CAS AI-Assisted Software Engineering (FFHS FS26)  ·  Andreas Imboden  ·  Februar 2026</text>
  <text x="600" y="945" text-anchor="middle" font-size="10.5" fill="#BDC3C7">Alle Services Self-Hosted auf Proxmox  ·  Anthropic API als KI-Fallback  ·  Docker Compose Deployment</text>
</svg>


### 6.2 Hybrid Skill-System (Kernarchitektur)

FlowHub verwendet einen **Hybrid-Ansatz** für das Skill-System, der Code und Konfiguration kombiniert:

**`SKILL.md`** (Deklarativ, versioniert):

```yaml
---
name: article-skill
description: Saves articles for later reading to Wallabag
handler: FlowHub.Skills.Handlers.ArticleSkillHandler
metadata:
  flowhub:
    triggers:
      keywords: [artikel, lesen, read later, später lesen]
      urlPatterns: [heise.de, ct.de, arstechnica.com]
    config:
      wallabag_tag: flowhub
---
# Dokumentation des Skills (für Menschen und KI lesbar)
```

**`ArticleSkillHandler.cs`** (Business-Logic, typsicher):

```csharp
public class ArticleSkillHandler : ISkillHandler
{
    private readonly IWallabagClient _wallabag;

    public ArticleSkillHandler(IWallabagClient wallabag)
    {
        _wallabag = wallabag;
    }

    public async Task<SkillResult> ExecuteAsync(InputItem input, SkillConfig config)
    {
        await _wallabag.SaveEntryAsync(input.Url, config.WallabagTag);
        return new SkillResult { Success = true };
    }
}
```

**Vorteile dieses Ansatzes:**

- Konfiguration ohne Neustart änderbar (`SKILL.md`)
- Typsichere Business-Logic (C#)
- Selbstdokumentierend (Markdown human readable)
- Zukunftsfähig: KI kann `SKILL.md` Files lesen und generieren

### 6.3 Technologie-Stack

| Schicht | Technologie | Begründung |
|---|---|---|
| Backend | .NET 10 / C# / ASP.NET Core | LTS-Release Nov. 2025, vertrauter Stack, hohe Performance |
| Web-UI | Blazor SSR (Server-Side Rendering) | .NET-native, kein JS-Framework nötig, SEO-freundlich |
| KI-Integration | Microsoft.Extensions.AI + Ollama / Anthropic SDK | Abstraktion über LLM-Provider, Ollama: lokal & kostenlos |
| Datenbank | PostgreSQL 16 | Bewährt, EF Core Integration |
| Cache/State | Redis 7 | Session-State, Pending Inputs |
| API-Framework | ASP.NET Core Minimal APIs | Schlank, performant, .NET-nativ |
| REST Clients | Refit | Typsicher, deklarativ, Interface-basiert |
| ORM | Entity Framework Core 10 | .NET-Standard, Code-First Migrations |
| Telegram | Telegram.Bot (NuGet) | Offiziell unterstützte .NET-Library |
| Deployment | Docker Compose | Einfach, Proxmox-kompatibel |

### 6.4 Infrastruktur (Proxmox Homelab)

```
Proxmox VE
+-- VM: Docker Host
|   +-- FlowHub Backend (.NET 10)
|   +-- PostgreSQL
|   +-- Redis
|   +-- Ollama (Llama 3.x)
|
+-- VM/LXC: Wallabag        (Self-Hosted Read-Later)
+-- VM/LXC: paperless-ngx   (bereits vorhanden)
+-- VM/LXC: Vikunja         (Task-Management / Kanban)
+-- VPS (extern, DE):
    +-- GitLab              (Obsidian Markdown Repo)

Cloud:
+-- Anthropic API           (Claude, Fallback für KI)
```

---

## 7. Architekturentscheidungen (ADR)

Dieses Kapitel listet die **frühen Plattform- und Strategie-Entscheidungen** aus der Konzeptphase (vor Block 1). Sie definieren den Rahmen für die spätere Implementierung. Während der Umsetzung sind sechs **lightweight Implementation-ADRs** (Context → Decision → Consequences) im Repository unter [`docs/adr/`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/adr) entstanden, die dort einzeln einsehbar sind:

| ADR | Titel | Status |
|---|---|---|
| [0001](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0001-frontend-render-mode-and-architecture.md) | Frontend Render Mode and Architecture | Accepted |
| [0002](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0002-service-architecture-and-async-communication.md) | Service Architecture and Async Communication | Accepted |
| [0003](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0003-async-pipeline.md) | Async Pipeline (MassTransit) | Accepted |
| [0004](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0004-ai-integration-in-services.md) | AI Integration — Provider Abstraction (MEAI) | Accepted |
| [0005](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0005-persistence.md) | Persistence — EF Core + PostgreSQL | Accepted |
| [0006](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0006-vector-search.md) | Vector Search — pgvector + Mistral Embeddings | Accepted |

Die nachfolgenden Einträge **PE-1 bis PE-7** (Plattform-Entscheidungen) sind bewusst von den Implementation-ADRs entkoppelt — sie sind Strategie, nicht Lösungsstruktur.

### PE-1: C# / .NET 10 als primäre Sprache und Plattform

**Entscheidung:** C# mit .NET 10 als primäre Sprache.

**Begründung:**
- Vertrauter Stack aus dem beruflichen Hintergrund (geringere Lernkurve)
- .NET 10 als aktueller LTS-Release (erschienen November 2025) mit langfristigem Support
- Starkes Ökosystem: EF Core, ASP.NET Core, Refit, Blazor – alles aus einem Guss
- Null-Safety via Nullable Reference Types (C# 8+)
- In der PVA explizit bestätigt: freie Wahl des Technologie-Stacks

**Konsequenz:** Kein Wechsel zu Quarkus/Kotlin nötig; vorhandenes C#-Wissen direkt produktiv einsetzbar.

### PE-2: Blazor SSR für das Web-Dashboard

**Entscheidung:** Blazor Server-Side Rendering (SSR) für die Admin-Ansicht.

**Begründung:**
- .NET-native, kein separates JavaScript-Framework (React, Angular, Vue) nötig
- SSR liefert fertig gerendertes HTML – schnell, SEO-freundlich, kein WASM-Download
- Komponenten-Modell vertraut aus dem .NET-Ökosystem
- Volle C#-Typsicherheit auch im Frontend

**Konsequenz:** Blazor SSR eignet sich optimal für das Admin-Dashboard; für komplexe interaktive Elemente kann Blazor Interactive Server Mode ergänzt werden.

### PE-3: Hybrid Skill-System (SKILL.md + C# Handler)

**Entscheidung:** Kombination aus deklarativer Konfiguration (`SKILL.md`) und typsicherer Business-Logic (C#).

**Begründung:**
- Pure Code: Gut testbar, keine Runtime-Konfiguration
- Pure Config: Flexibel, aber keine Typsicherheit
- Hybrid: Best of Both Worlds

**Konsequenz:** Zwei Artefakte pro Skill (`SKILL.md` + Handler), aber maximale Flexibilität.

### PE-4: Docker Compose jetzt, k3s als Future Option

**Entscheidung:** Docker Compose für MVP-Deployment.

**Begründung:**
- Schnellerer Start (1 Tag Setup vs. 1 Woche k8s)
- Einfacheres Debugging während Entwicklung
- Passt zu FFHS Block 5 Timeline (k8s im Juni)
- App ist von Beginn an k8s-ready (12-Factor, Health Checks, Stateless)

**Konsequenz:** Migration zu k3s als Block 5 Projekt möglich, ohne Code-Änderungen.

### PE-5: Ollama (lokal) als primäres LLM

**Entscheidung:** Llama 3.x via Ollama lokal, Anthropic API als Fallback.

**Begründung:**
- Ollama: Kostenlos, privat, offline-fähig
- Anthropic API: ~$6/Monat für Homelab-Nutzung, nur für hochwertige Aufgaben
- `Microsoft.Extensions.AI`: Provider-Wechsel nur via Config, kein Code-Änderung

**Konsequenz:** 4.7 GB Model-Download, 4 GB RAM Bedarf für Ollama.

### PE-6: paperless-ngx für Dokumenten-MVP als reines Upload-Ziel

**Entscheidung:** Im MVP fungiert paperless-ngx als Dokumenten-Archiv ohne KI-Analyse.

**Begründung:**
- Fokus auf funktionierende Grundstruktur, nicht Feature-Tiefe
- OCR und Metadaten-Extraktion sind komplex (separates Lernziel)
- paperless-ngx hat eigene gute Basis-Kategorisierung via Regeln

**Konsequenz:** DocumentSkill macht nur: Erkennen → Upload. Keine Tags, keine Metadaten-Extraktion im MVP.
**Future:** KI-gestützte Analyse des OCR-Texts (Betrag, Datum, Händler) via `Microsoft.Extensions.AI`.

### PE-7: Kein Cloud-SaaS – ausschliesslich Self-Hosted Services

**Entscheidung:** Alle Ziel-Services laufen Self-Hosted im Homelab. Todoist (Cloud SaaS) wird nicht integriert.

**Begründung:**
- Datensouveränität: Alle persönlichen Daten bleiben im eigenen Homelab
- Kostenfreiheit: Keine laufenden SaaS-Gebühren
- Vikunja ersetzt Todoist vollständig für Task/Item-Management und übernimmt auch die Kanban-Sicht für Homelab-Services

**Konsequenz:** Alle 4 Ziel-Services (Wallabag, Vikunja, paperless-ngx, GitLab) sind Self-Hosted auf Proxmox.

---

## 8. CAS-Kursstruktur und Projektfortschritt

**Mapping: FFHS Blöcke → FlowHub Entwicklung**

| Block | Datum | FFHS Thema | FlowHub Deliverable |
|---|---|---|---|
| 1 | Feb 21 | Konzeption, Einführung | ✅ Projektbeschreibung, Architekturentwurf, ADRs |
| 2 | Mär 21 | Frontend, Web-Präsentation | Telegram Bot UI, Blazor SSR Dashboard (Admin-Ansicht) |
| 3 | Apr 25 | Services, REST, MCP | Skill-System, REST Clients via Refit (Wallabag, Vikunja, paperless) |
| 4 | Mai 23 | Persistence, Datenbanken | PostgreSQL Schema via EF Core, Redis State, Repository-Pattern |
| 5 | Jun 20 | Docker, Kubernetes | Docker Compose finalisiert, optional: k3s Migration |
| Abgabe | Jul 6 | – | Vollständiges MVP + Dokumentation + Git-Repository |

---

## 9. KI-Einsatz in der Entwicklung (Reflexion)

**Detaillierte Auswertung pro Block / Slice:** `docs/ai-usage.md` (531 Zeilen, gegliedert pro Block mit *generated-vs-handwritten*-Tabellen, Korrektur-Geschichten und Reflexion). Dieser Abschnitt fasst die Erkenntnisse über alle fünf Blöcke zusammen.

### KI-Tools im Entwicklungsprozess

| Werkzeug | Einsatzbereich |
|---|---|
| Claude Code (Opus 4.7, 1M-Kontext) | Brainstorming, Spec/Plan-Erstellung, ADR-Drafts, Controller für Subagent-Dispatches |
| Claude Sonnet 4.6 (Subagents) | Implementer + Spec-Reviewer + Code-Quality-Reviewer unter dem `superpowers:subagent-driven-development`-Workflow |
| Claude Haiku 4.5 (Subagents) | Mechanische Tasks (csproj/Markdown/YAML), ca. 60 % Token-Ersparnis gegenüber Sonnet |
| `/ultrareview` (Multi-Agent Branch-Review) | Architektonisches Review über ganzen Feature-Branch — fängt System-Invarianten ab, die Per-Task-Review nicht sieht |
| Microsoft.Extensions.AI + Anthropic/OpenRouter | KI im Produkt (Classifier, Quote-Enricher, Embeddings) |
| Eigenentwickelte CAS-Skills | `cas-aise-todo-list`, `cas-aise-grade-self-check`, `sync-ai-instructions` — Rubrik-Verankerung, Block/Phase-Steuerung, Cross-Project-Reuse |

### Workflow-Entwicklung (Block 1 → Block 5)

Mit jedem Block wurde ad-hoc Chat gegen strukturierteren, isolierteren Subagent-Dispatch getauscht. Der Mehraufwand für Struktur hat sich durch weniger Mid-Task-Eskalationen und seltenere "Agent ist abgedriftet"-Momente bezahlt gemacht.

| Block | Workflow-Verschiebung |
|---|---|
| 1 — Einführung | Ad-hoc Claude.ai-Chat für Architektur; Copilot inline für Code. |
| 2 — Frontend | Erster sustained Claude-Code-CLI-Einsatz mit phasenbasierter Disziplin (`/ui-brainstorm` → `/ui-flow` → `/ui-build` → `/ui-review`). |
| 3 — Service | Erster vollständiger `superpowers:brainstorming` → `writing-plans` → `subagent-driven-development`-Slice. Zweistufiges Review (Spec + Quality) pro Task. |
| 4 — Persistence | Sonnet als Default, Haiku für Mechanik; Per-Task-Quality-Review fallen gelassen zugunsten einer Branch-Review am Slice-Ende — ca. 60 % Token-Burn reduziert ohne Qualitätsverlust. |
| 5 — Deployment | `/ultrareview` (Multi-Agent Branch-Review) und rubrik-gegroundetes `cas-aise-grade-self-check` ergänzt; jede Block-Abschluss-Prüfung läuft durch die Skill. |

Bis Block 5 verantworten Implementer-Subagents ganze Slices end-to-end. Die menschliche Arbeit konzentriert sich an zwei Stellen: **Spec** (wo Verträge festgenagelt werden) und **Review** (wo System-Invarianten geprüft werden). Der Engpass wandert vom Tippen zu diesen beiden Punkten.

### Wiederkehrende Fehlerquellen

1. **Single-Pass-AI-Review übersieht System-Invarianten.** Block-5-Beispiel: AI-generierter Code, der Embedding-Generation in `EfCaptureService.SubmitAsync` integrierte, war lokal korrekt (eine Transaktion) — global aber falsch, weil ein langsamer Provider das NfA-09 p95 < 200 ms Submit-Budget sprengt. Per-Task-Quality-Review konnte das nicht fangen; `/ultrareview` wurde dafür eingeführt und hat es beim ersten Lauf gefunden.
2. **Deployment-Shape-Failures sind systematisch unter-getestet.** Compose-`${X:-}`-Interpolation substituiert leere Strings (nicht null), wodurch `??`-Defaults still no-op'en. `.editorconfig` nicht in den Docker-Build-Kontext kopiert → Analyzer-Suppressions werden zu Build-Errors. Casing-Mismatch `EMBEDDINGS__APIKEY` vs. `Embeddings__ApiKey` schaltete ein ganzes Feature aus. Fünf solcher Defekte in einem Nachmittag durch `just smoke-prod` gefangen.
3. **Training-Data-Lag bei aktuellen Libraries.** `Pgvector.EntityFrameworkCore` wurde als in-the-box-Npgsql-10-Feature angenommen (ist aber ein separates Paket). `MassTransit.Testing` wurde als separates Paket angenommen (ist Teil der Haupt-Assembly). API-Behauptungen der KI müssen mechanisch gegen das tatsächlich installierte Paket verifiziert werden.
4. **Offene Prompts inflationieren den Scope.** "Füge X hinzu" ohne Plan produziert eine Feature-Suite. Das Gegenmittel war `superpowers:writing-plans` — kleinteilige TDD-geordnete Tasks mit vollständigem Inline-Code, exakten Pfaden und Commit-Messages. Subagents gegen wohldefinierte Tasks meldeten DONE; Subagents gegen offene Prompts gingen explorieren.
5. **Pläne aus unvollständigem Repo-Wissen.** Block-3-Slice-B-Plan übersah `CaptureServiceStubTests.cs` und die existierenden `ChannelKind`-Werte; Implementer mussten mid-Task adaptieren. Brainstorming/Planning liest seitdem jede referenzierte Datei *bevor* der Plan eingefroren wird.

### Wiederkehrende Stärken

1. **Der Plan ist der Vertrag.** Enthält der Plan exakte Pfade, exakten Code, exakte Commit-Messages und exakte Verifikationskommandos, operiert der Implementer mit engem Judgment-Spielraum und meldet DONE statt NEEDS_CONTEXT. Die 95 %+ "AI-drafted"-Anteile im Produktionscode sind nur deshalb erreichbar.
2. **Review-Kadenz muss zur Fehlerklasse passen.** Per-Task-Spec + Per-Task-Quality war für Block 3 Slice B richtig (erster SDD-Lauf). Ab Block 4 fing Per-Task-Quality nichts Neues mehr, während ein Branch-weiter `/ultrareview` am Slice-Ende genau die Architektur-Fehler fand, die Per-Task-Review nicht sehen konnte.
3. **Eigene Skills als Memory-Layer des Projekts.** `cas-aise-todo-list`, `cas-aise-grade-self-check`, `sync-ai-instructions` decken Concerns ab, die das Upstream-`superpowers`-Plugin nicht behandelt: Rubrik-Verankerung, kalender-getriebene Priorisierung, Cross-Project-Instruction-Reuse. Ohne `cas-aise-grade-self-check` driftete die Rubrik-Abdeckung in langen Sessions — die Skill fand fehlende Evidenz in genau dieser Reflexion vor Abgabe.
4. **KI für rigide-Struktur-Artefakte, Mensch für architektonisches Urteil.** GitHub-Actions-YAML, ADR-Scaffolds, OpenAPI-ProblemDetails, EF-Migrationen — rigide Strukturen, in denen der 90-%-korrekte AI-Erstwurf reale Zeit spart. Architektur-Entscheidungen (hexagonaler Split für AI, In-Process vs. RabbitMQ Transport, Dispatcher-Event-Carries-Description vs. Inline-Skill-Call) bleiben beim Menschen; die KI listet Alternativen, sie wählt nicht.
5. **Korrektur-Geschichten sind die Rubrik-Evidenz, die zählt.** `docs/insights/block-5.md` "Defects Found by the Smoke Run" und die "Ultrareview-driven correction" / "Smoke-driven correction"-Sektionen in `docs/ai-usage.md` sind auditierbar: konkreter Defekt, fixender Commit, Lehre. Pauschale "KI hat geholfen"-Aussagen schliessen keinen Rubrik-Loop; "KI produzierte X, Smoke fing Y, Fix landete in Commit Z" schliesst ihn.

### Was ich anders machen würde

- **Rubrik-Verankerung ab Block 1, nicht Block 3.** `cas-aise-grade-self-check` entstand mittendrin; Block 1–2 hatten keine formalen `use-cases.md` / `nfa.md` / `acceptance-criteria.md`. Retroaktive Doku-Arbeit in spätem Block 3 / frühem Block 4 kostete reale Zeit.
- **`/ultrareview` ab Block 3, nicht Block 5.** Branch-weites Multi-Agent-Review fängt die Klasse von Architektur-Fehlern, die Per-Task-Review nicht sieht. Günstiger nach Slice B als nach einem 21-Task-Beta-MVP.
- **Ein `just smoke`-Rezept pro Block, nicht nur Block 5.** Fünf latente Bugs in einem Nachmittag durch `just smoke-prod`. Ein einfacheres Smoke (App booten, Feature-Pfad curlen) hätte mindestens drei davon früher gefangen.
- **`InternalsVisibleTo`- und EF-Core-Test-Host-Pattern im Plan-Template kodifizieren.** Block 3 Slice D und Block 4 hatten beide Implementer, die bei Compile-Errors per Default Sichtbarkeit aufweiteten. Das richtige Pattern ist mechanisch — sobald im Plan ausgeschrieben verschwindet die Friktion.

### Hat die Ausgangshypothese gestimmt?

Die Block-1-Annahme war: Claude ist "ein schneller Tipper mit guter Library-Kenntnis" — nützlich für Boilerplate, suspekt bei Architektur, im Wesentlichen ein Produktivitäts-Multiplikator und keine Workflow-Änderung. Bis Block 5 ist das in beide Richtungen weit daneben.

**Boilerplate ist schneller als erwartet.** Der 95 %+ AI-drafted-Anteil im Produktionscode ist nicht "KI hat 95 % der Arbeit gemacht" — die 5 % Menschen-Input konzentrieren sich auf High-Judgment-Punkte (Scope, Verträge, Trade-offs). Das eigentliche Tippen ist aber wirklich ein kleiner Bruchteil der verstrichenen Zeit. Der Engpass wandert von der Implementation zu Spec und Review — exakt die Umkehrung, auf die SDD optimiert.

**Architektur ist gefährlicher als erwartet.** Der Embedding-on-Submit-Fall ist ein Ein-Absatz-Rewrite, der im Nachhinein offensichtlich ist: KI produzierte lokal korrekten Code, der eine System-Invariante brach, und ein Single-Pass-Review hätte ihn ausgeliefert. Die Deployment-Shape-Failures haben die gleiche Form: lokal korrekt, global falsch. Die menschliche Rolle an der Architektur-Grenze ist nicht geschrumpft, sie ist gewachsen — weil der Agent jetzt genug Code schnell genug produziert, dass nur noch der Filter an Design und Review Sinn ergibt.

KI hat keine Rolle im Workflow ersetzt. Sie hat verschoben, welche Rolle der Engpass ist. Für das nächste Projekt: zuerst die Spec-Dokumente, `/ultrareview` ab Tag eins, ein Smoke-Target vor dem ersten Feature. Implementations-Durchsatz ist nicht mehr die Beschränkung — Design- und Review-Durchsatz sind es.

---

## 10. Abgrenzung: Was FlowHub nicht ist

- ❌ Kein allgemeiner Chatbot – FlowHub hat eine klar definierte Aufgabe
- ❌ Kein Ersatz für paperless-ngx / Vikunja / Wallabag – FlowHub ergänzt, ersetzt nicht
- ❌ Kein IFTTT/n8n Clone – FlowHub ist code-basiert, nicht no-code
- ❌ Keine Multi-User-Plattform – Single-User Homelab-Tool
- ❌ Keine Cloud-Abhängigkeiten – ausschliesslich Self-Hosted

---

## 11. Risiken und Mitigationen

| Risiko | Wahrscheinlichkeit | Mitigation |
|---|---|---|
| Scope Creep (zu viele Skills) | Hoch | Klare MVP-Liste, Future klar abgegrenzt |
| Blazor SSR Lernkurve | Niedrig | Bekanntes C#-Ökosystem; gute Microsoft-Dokumentation |
| Ollama zu langsam (kein GPU) | Mittel | Claude API als Fallback; Keyword-Detection als Basis |
| Telegram API Änderungen | Niedrig | Abstraktion via ISkillHandler Interface |
| Vikunja API-Änderungen | Niedrig | Refit-Abstraktion, versionierte API-Clients |
| Zeitdruck (Abgabe Juli) | Mittel | MVP bewusst schlank, Future Features dokumentiert |

---

## 12. Glossar

| Begriff | Bedeutung |
|---|---|
| Skill | Handler für einen bestimmten Input-Typ (z.B. ArticleSkill) |
| SKILL.md | Konfigurationsdatei eines Skills (YAML Frontmatter + Markdown) |
| ISkillHandler | C# Interface mit der Business-Logic eines Skills |
| SkillRegistry | Lädt alle SKILL.md Files und zugehörige Handler |
| Skill-Vorschlag | Feature: System schlägt neuen Skill vor wenn kein bestehender passt |
| Pending Input | Input der auf Benutzer-Bestätigung wartet (in Redis) |
| Confidence | Wie sicher die KI bei der Skill-Erkennung ist (0.0–1.0) |
| Enrichment | KI-gestützte Anreicherung (z.B. Autor/AuthorInfo bei QuoteSkill) |
| Homelab | Selbst betriebene Server-Infrastruktur zu Hause (Proxmox) |
| Blazor SSR | Server-Side Rendering mit Blazor (.NET); HTML wird serverseitig gerendert |
| EF Core | Entity Framework Core – .NET ORM für Datenbankzugriffe |
| Refit | Typsichere REST-Client-Library für .NET (deklarativ via Interfaces) |
| M.E.AI | Microsoft.Extensions.AI – Abstraktion für LLM-Provider in .NET |
| Vikunja | Self-Hosted Task-Management / Kanban (ersetzt Todoist) |
| Wallabag | Self-Hosted Read-Later Service für Artikel |
| Knowledge Base | Obsidian Markdown Files, versioniert in GitLab (Self-Hosted) |

---

*Erstellt mit Unterstützung von Claude (Anthropic) – gemäss FFHS Richtlinien für KI-Einsatz in Projektarbeiten.*
