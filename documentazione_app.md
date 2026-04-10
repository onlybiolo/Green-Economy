# DOCUMENTAZIONE TECNICA - GREEN ECONOMY APP

## COMPONENTI DELL'APPLICAZIONE

### FORM PRINCIPALE (Form1.cs)
Rappresenta il modulo principale dell'applicazione e funge da orchestratore tra l'utente e i servizi di backend.
- **Welcome Screen**: Schermata di ingresso che gestisce l'inizializzazione dei bottoni principali e degli effetti grafici (Paint Event) per il titolo e lo sfondo.
- **Navigazione Tab-based**: Sistema di navigazione interno che permette lo switch dinamico tra i diversi pannelli funzionali senza dover aprire nuove finestre.
- **Event Handler Linguistico**: Un sistema basato su eventi (`OnLanguageChanged`) che aggiorna tutte le stringhe dell'interfaccia in tempo reale caricando file JSON codificati in ISO.

### PANEL DASHBOARD (Contenitore Funzionale)
Il cuore operativo dell'applicazione, suddiviso in moduli logici:
- **Modulo Mappa (Leaflet Logic)**: Pannello che ospita il controllo WebView2. Gestisce la visualizzazione geografica e riceve messaggi asincroni dal Javascript per il piazzamento dei marker.
- **Modulo Analisi Statistica**: Gestisce il calcolo della Correlazione di Pearson e la generazione dinamica dei grafici tramite dati aggregati dal database.
- **Modulo DataGrid**: Visualizzazione tabellare raw dei dati collezionati, con ordinamento cronologico discendente.

### FORM IMPOSTAZIONI (FormImpostazioni.cs)
Modulo dedicato alla persistenza delle preferenze utente.
- Gestisce il salvataggio cifrato (Logica Magic Word) e in chiaro delle API Key.
- Permette la configurazione della città di default tramite il file di configurazione `settings.json`.

---

## DIPENDENZE E RISORSE ESTERNE

### LITEDB (Database Engine)
Scelto per la sua natura **Serverless** e la perfetta integrazione con l'ambiente .NET.
- **Architettura**: Database orientato ai documenti (NoSQL) che non richiede installazioni di servizi esterni.
- **Utilizzo**: Gestisce la persistenza a lungo termine dei record ambientali e funge da strato di caching per ottimizzare le richieste di rete.

### OPENWEATHERMAP (Weather & Geocoding Service)
- **Geocoding API**: Utilizzata per la normalizzazione degli input utente da stringhe testuali a coordinate geografiche (Lat/Lon).
- **Weather API**: Fornisce i parametri meteorologici attuali e storici tramite protocollo REST.

### IQAIR / AIRVISUAL (Air Quality API)
- Servizio specializzato nel monitoraggio ambientale. Fornisce l'indice AQI e identifica il principale inquinante atmosferico (Main Pollutant) presente in una specifica coordinata.

### WEBVIEW2 & LEAFLET.JS (Mapping Kit)
- **Logica Ibrida**: Utilizzo di un controllo WebView2 (Chromium-based) per l'esecuzione di una Web App locale (Assets/map.html).
- **Leaflet**: Libreria Javascript utilizzata per la manipolazione di layer cartografici e la gestione di eventi geografici interattivi.

---

## FUNZIONI DI MANUTENZIONE E EXPORT
- **DB Reset**: Procedura di pulizia della collezione LiteDB per il ripristino dei dati iniziali.
- **Data Export (CSV)**: Logica di esportazione implementata nella classe `DataExporter` che permette di convertire il database in un formato leggibile per analisi esterne tramite Excel o software statistici.
