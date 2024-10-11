using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Instrumentation;
using System.Net;
using System.Net.Http;
using System.Text;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;
using PL.Dati;
using PL.iSell.Utilita;
using PL.iSell.Utilita.RigheDatiiSell;
using PL.iSell.Utilita.ValoriTabelle;
using PL.iSell.Utilita.ValoriTabelle.CausaliRigheDocumenti;
using PL.iSell.Utilita.ValoriTabelle.ContattiAnagrafiche;
using PL.iSell.Utilita.ValoriTabelle.GruppiAnagrafiche;
using PL.iSell.Utilita.ValoriTabelle.GruppiArticoli;
using PL.iSell.Utilita.ValoriTabelle.RelazioniAnagrafiche;
using PL.iSell.Utilita.ValoriTabelle.TipiDocumenti;
using PL.Utilita;
using PL.Utilita.ParametriConfigurazione;
using AttivitaPassivita = PL.iSell.Utilita.ValoriTabelle.Listini.AttivitaPassivita;
using GestioneLotti = PL.iSell.Utilita.ValoriTabelle.Articoli.GestioneLotti;
using TipoValorizzazionePrezzi = PL.iSell.Utilita.ValoriTabelle.Listini.TipoValorizzazionePrezzi;

namespace PL.iSell.Connettori.BMInformatica.SMART
{
    public class Connettore : PL.iSell.Connettori.Connettore
    {
        #region Costanti

        private const string PREFISSO_GRUPPI_ARTICOLI_MARCHE = "ISELL_GRP_ART_MARCHE\\";
        private const string PREFISSO_GRUPPI_ARTICOLI_CATEGORIE_MERCEOLOGICHE = "ISELL_GRP_ART_CATEGORIE_MERCEOLOGICHE\\";

        private const string ID_GRUPPO_ARTICOLI_SUPERIORE_MARCHE = "Marche";
        private const string ID_GRUPPO_ARTICOLI_SUPERIORE_GRUPPO_MERCEOLOGICO = "GruppoMerceologico";

        protected const string ID_TIPO_DOCUMENTO_ORDINE_STORICO = "ORD";

        protected const string ID_TIPO_DOCUMENTO_FATTURA_IMMEDIATA = "FT_IMM";
        protected const string ID_TIPO_DOCUMENTO_FATTURA_DIFFERITA = "FT_DIF";
        protected const string ID_TIPO_DOCUMENTO_FATTURA_ACCOMPAGNATORIA = "FT_ACC";
        protected const string ID_TIPO_DOCUMENTO_NOTA_CREDITO = "NC";
        protected const string ID_TIPO_DOCUMENTO_DDT = "DDT";
        protected const string ID_TIPO_DOCUMENTO_DDT_FORNITORI = "DDT_FOR";

        protected const string ID_CAUSALE_RIGA_DOCUMENTO_VENDITA = "BM_normale";
        protected const string ID_CAUSALE_RIGA_DOCUMENTO_SCONTO_MERCE = "BM_sconto_merce";
        protected const string ID_CAUSALE_RIGA_DOCUMENTO_OMAGGIO_TOTALE = "BM_omaggio_no_rivalsa";
        protected const string ID_CAUSALE_RIGA_DOCUMENTO_OMAGGIO_IMPONIBILE = "BM_omaggio";

        #endregion

        #region Variabili

        public ConnessioneDati ConnessioneSmart { get; private set; }

        #endregion

        #region Costruttore

        public Connettore() : base(
            "BMInformatica.Smart",
            FasiElaborazioni.AnagraficheOUT,
            FasiElaborazioni.ArticoliOUT,
            FasiElaborazioni.DocumentiOUT,
            FasiElaborazioni.ListiniOUT,
            FasiElaborazioni.DocumentiIN,
            FasiElaborazioni.StatisticheOUT
        )
        {
            this.Produttore = "BMInformatica";
            this.Prodotto = "SMART";

            this.ConnessioneSmart = this.RegistraConnessione("Smart", "Smart", TipiProvider.MySQL);

            Dictionary<int, string> valoriDaProporre = new Dictionary<int, string>();
            valoriDaProporre.Add(0, "Non visualizzare");
            valoriDaProporre.Add(1, "Visualizza gruppi sotto gruppo principale");
            valoriDaProporre.Add(2, "Visualizza gruppi senza gruppo principale");

            Dictionary<int, string> valoriConsentiModificaSconto = new Dictionary<int, string>();
            valoriConsentiModificaSconto.Add(0, "Non consentire modifica sconto (default)");
            valoriConsentiModificaSconto.Add(1, "Consenti modifica sconto");

            var parametroServer = this.NuovoGruppoParametri("Server");
            parametroServer.AggiungiParametri(
                new ParametroTesto("URLApi", "URL Api inserimento documenti", "", "")
            );

            var parametroApiImprt = this.NuovoGruppoParametri("Api");
            parametroApiImprt.AggiungiParametri(
                new ParametroTesto("apiKey", "apiKey", "", ""),
                new ParametroTesto("password", "Password", "", "")
            );

            var parametroArticoli = this.NuovoGruppoParametri("Articoli");

            parametroArticoli.AggiungiParametri(new ParametroNumeroIntero("GruppiMerceologici", "Visualizza gruppi merceologici", "Visualizza gruppi merceologici", "", 1, valoriDaProporre, true)
            );
            parametroArticoli.AggiungiParametri(new ParametroNumeroIntero(
                "GruppiMarche",
                "Visualizza gruppi marche", "Visualizza gruppi marche",
                "",
                1,
                valoriDaProporre,
                true)
            );

            var parametroSconti = this.NuovoGruppoParametri("Sconti");
            parametroSconti.AggiungiParametri(new ParametroNumeroIntero(
                    "ConsentiModificaSconti",
                    "Consenti modifica sconti",
                    "Se abilitata la modifica degli sconti, vengono creati nuovi codici nella tabella tsm in caso non venga rilevata la combinazione.",
                    "",
                    0,
                    valoriConsentiModificaSconto,
                    true
                )
            );

            #region Query Anagrafiche

            this.RegistraQuery("Anagrafiche", @"
SELECT
    cli.CODICE,
    cli.DESCRIZIONE1,
    cli.DESCRIZIONE2,
    cli.VIA,
    nom.CAP,
    cli.CITTA,
    nom.provincia,
    TCC_CODICE,
    cli.PARTITA_IVA,
    cli.CODICE_FISCALE,
    TIV_CODICE,
    cli.NOTE,
    TPA_CODICE,
    TLV_CODICE,
    FIDO,
    nom.E_MAIL_AMMINISTRAZIONE,
    nom.TELEFONO,
    nom.CELLULARE,
    nom.FAX,
    nom.WEB,
    nom.E_MAIL_PEC,
    cli.OBSOLETO
FROM
    cli
INNER JOIN
    nom
ON
    cli.CODICE = nom.CODICE
WHERE
    cli.OBSOLETO = 'no'
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDAnagrafica", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE"),
                new ColonnaParametroTabella("DescrizioneAnagrafica", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE1"),
                new ColonnaParametroTabella("DescrizioneAggiuntivaAnagrafica", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE2"),
                new ColonnaParametroTabella("Indirizzo", ColonnaParametroTabella.TipiDati.Alfanumerico, "VIA"),
                new ColonnaParametroTabella("CAP", ColonnaParametroTabella.TipiDati.Alfanumerico, "CAP"),
                new ColonnaParametroTabella("Citta", ColonnaParametroTabella.TipiDati.Alfanumerico, "CITTA"),
                new ColonnaParametroTabella("Stato", ColonnaParametroTabella.TipiDati.Alfanumerico, "TCC_CODICE"),
                new ColonnaParametroTabella("Provincia", ColonnaParametroTabella.TipiDati.Alfanumerico, "provincia"),
                new ColonnaParametroTabella("PartitaIVA", ColonnaParametroTabella.TipiDati.Alfanumerico, "PARTITA_IVA"),
                new ColonnaParametroTabella("CodiceFiscale", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE_FISCALE"),
                new ColonnaParametroTabella("IDAliquotaIVA", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIV_CODICE"),
                new ColonnaParametroTabella("Note", ColonnaParametroTabella.TipiDati.Alfanumerico, "NOTE"),
                new ColonnaParametroTabella("IDPagamento", ColonnaParametroTabella.TipiDati.Alfanumerico, "TPA_CODICE"),
                new ColonnaParametroTabella("IDListino", ColonnaParametroTabella.TipiDati.Alfanumerico, "TLV_CODICE"),
                new ColonnaParametroTabella("Fido", ColonnaParametroTabella.TipiDati.NumeroDecimale, "FIDO"),
                new ColonnaParametroTabella("Mail", ColonnaParametroTabella.TipiDati.NumeroDecimale, "E_MAIL_AMMINISTRAZIONE"),
                new ColonnaParametroTabella("Telefono", ColonnaParametroTabella.TipiDati.NumeroDecimale, "TELEFONO"),
                new ColonnaParametroTabella("Cellulare", ColonnaParametroTabella.TipiDati.NumeroDecimale, "CELLULARE"),
                new ColonnaParametroTabella("Fax", ColonnaParametroTabella.TipiDati.NumeroDecimale, "FAX"),
                new ColonnaParametroTabella("SitoInternet", ColonnaParametroTabella.TipiDati.NumeroDecimale, "WEB"),
                new ColonnaParametroTabella("PEC", ColonnaParametroTabella.TipiDati.NumeroDecimale, "E_MAIL_PEC")
            );
            this.RegistraQuery("DestinazioniAnagrafiche", @"
SELECT
    CLI_CODICE,
    INDIRIZZO,
    DESCRIZIONE1,
    DESCRIZIONE2,
    VIA,
    CAP,
    CITTA,
    provincia,
    NOTE,
    OBSOLETO
FROM
    ind
WHERE
    OBSOLETO = 'no'
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDAnagraficaSuperiore", ColonnaParametroTabella.TipiDati.Alfanumerico, "CLI_CODICE"),
                new ColonnaParametroTabella("IDAnagraficaDestinazione", ColonnaParametroTabella.TipiDati.Alfanumerico, "INDIRIZZO"),
                new ColonnaParametroTabella("DescrizioneAnagrafica", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE1"),
                new ColonnaParametroTabella("DescrizioneAggiuntivaAnagrafica", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE2"),
                new ColonnaParametroTabella("Indirizzo", ColonnaParametroTabella.TipiDati.Alfanumerico, "VIA"),
                new ColonnaParametroTabella("CAP", ColonnaParametroTabella.TipiDati.Alfanumerico, "CAP"),
                new ColonnaParametroTabella("Citta", ColonnaParametroTabella.TipiDati.Alfanumerico, "CITTA"),
                new ColonnaParametroTabella("Provincia", ColonnaParametroTabella.TipiDati.Alfanumerico, "provincia"),
                new ColonnaParametroTabella("Note", ColonnaParametroTabella.TipiDati.Alfanumerico, "NOTE")
            );
            this.RegistraQuery("Operatori", @"
SELECT 
    CODICE,
    DESCRIZIONE
FROM
    tag
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDOperatore", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE"),
                new ColonnaParametroTabella("DescrizioneOperatore", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE")
            );

            #endregion

            #region Query Articoli

            this.RegistraQuery("Articoli", @"
SELECT
    art.codice,
    DESCRIZIONE1,
    DESCRIZIONE2,
    TUM_CODICE,
    TIV_CODICE_VENDITE,
    LOTTI,
    NOTE,
    TGM_CODICE,
    var_codice,
    TMR_CODICE,
    TGM_CODICE,
    OBSOLETO
FROM
    art
WHERE
    OBSOLETO = 'no'
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDArticolo", ColonnaParametroTabella.TipiDati.Alfanumerico, "codice"),
                new ColonnaParametroTabella("DescrizioneArticolo", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE1"),
                new ColonnaParametroTabella("DescrizioneAggiuntivaArticolo", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE2"),
                new ColonnaParametroTabella("IDUnitaDiMisura", ColonnaParametroTabella.TipiDati.Alfanumerico, "TUM_CODICE"),
                new ColonnaParametroTabella("IDAliquotaIVA", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIV_CODICE_VENDITE"),
                new ColonnaParametroTabella("GestioneLotti", ColonnaParametroTabella.TipiDati.Booleano, "LOTTI"),
                new ColonnaParametroTabella("Note", ColonnaParametroTabella.TipiDati.Alfanumerico, "NOTE"),
                new ColonnaParametroTabella("IDTipoArticolo", ColonnaParametroTabella.TipiDati.Alfanumerico, "TGM_CODICE"),
                new ColonnaParametroTabella("IDTipoVariantiArticoli", ColonnaParametroTabella.TipiDati.Alfanumerico, "var_codice"),
                new ColonnaParametroTabella("IDGruppoArticoliMarche", ColonnaParametroTabella.TipiDati.Alfanumerico, "TMR_CODICE"),
                new ColonnaParametroTabella("IDGruppoArticoliCategorieMerceologiche", ColonnaParametroTabella.TipiDati.Alfanumerico, "TGM_CODICE")
            );
            this.RegistraQuery("CategorieArticoli", @"
SELECT
    CODICE,
    DESCRIZIONE
FROM
    tgm
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDGruppoArticoli", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE"),
                new ColonnaParametroTabella("DescrizioneGruppoArticoli", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE")
            );

            this.RegistraQuery("MarcheArticoli", @"
SELECT
    CODICE,
    DESCRIZIONE
FROM
    tmr
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDGruppoArticoli", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE"),
                new ColonnaParametroTabella("DescrizioneGruppoArticoli", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE")
            );

            #endregion

            #region Query Documenti

            this.RegistraQuery("Ddt", @"
SELECT
    dvt.id,
    TIPO_DOCUMENTO,
    DATA_DOCUMENTO,
    numero_documento,
    CLI_CODICE,
    TIV_CODICE,
    TVA_CODICE,
    DATA_CONSEGNA,
    TPA_CODICE,
    TLV_CODICE,
    CLI_CODICE_VETTORE,
    NOTE,
    importo_acconto,
    NUMERO_COLLI,
    TSM_CODICE,
    TSM_CODICE_SCONTO,
    IMPORTO_TOTALE,
    IMPORTO_TOTALE_IVA,
    PROGRESSIVO
FROM
    dvt
LEFT JOIN
    tsm
ON
    dvt.TSM_CODICE_SCONTO = tsm.CODICE
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDDocumento", ColonnaParametroTabella.TipiDati.NumeroIntero, "id"),
                new ColonnaParametroTabella("IDTipoDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIPO_DOCUMENTO"),
                new ColonnaParametroTabella("DataDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "DATA_DOCUMENTO"),
                new ColonnaParametroTabella("NumeroDocumento", ColonnaParametroTabella.TipiDati.NumeroIntero, "numero_documento"),
                new ColonnaParametroTabella("IDAnagraficaIntestatario", ColonnaParametroTabella.TipiDati.Alfanumerico, "CLI_CODICE"),
                new ColonnaParametroTabella("IDValuta", ColonnaParametroTabella.TipiDati.Alfanumerico, "TVA_CODICE"),
                new ColonnaParametroTabella("IDAliquotaIVA", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIV_CODICE"),
                new ColonnaParametroTabella("DataConsegna", ColonnaParametroTabella.TipiDati.Alfanumerico, "DATA_CONSEGNA"),
                new ColonnaParametroTabella("IDPagamento", ColonnaParametroTabella.TipiDati.Alfanumerico, "TPA_CODICE"),
                new ColonnaParametroTabella("IDListino", ColonnaParametroTabella.TipiDati.Alfanumerico, "TLV_CODICE"),
                new ColonnaParametroTabella("IDVettore", ColonnaParametroTabella.TipiDati.Alfanumerico, "CLI_CODICE_VETTORE"),
                new ColonnaParametroTabella("Note", ColonnaParametroTabella.TipiDati.Alfanumerico, "NOTE"),
                new ColonnaParametroTabella("Acconto", ColonnaParametroTabella.TipiDati.NumeroDecimale, "importo_acconto"),
                new ColonnaParametroTabella("Colli", ColonnaParametroTabella.TipiDati.NumeroDecimale, "NUMERO_COLLI"),
                new ColonnaParametroTabella("CodiceSconto1", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE"),
                new ColonnaParametroTabella("CodiceSconto2", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE_SCONTO"),
                new ColonnaParametroTabella("ImponibileValorizzazione", ColonnaParametroTabella.TipiDati.NumeroDecimale, "IMPORTO_TOTALE"),
                new ColonnaParametroTabella("IVAValorizzazione", ColonnaParametroTabella.TipiDati.NumeroDecimale, "IMPORTO_TOTALE_IVA"),
                new ColonnaParametroTabella("Progressivo", ColonnaParametroTabella.TipiDati.NumeroIntero, "PROGRESSIVO")
            );
            this.RegistraQuery("RigheDdt", @"
SELECT
    dvr.id,
    dvt.numero_documento,
    dvr.ART_CODICE,
    dvr.DESCRIZIONE1,
    dvr.DESCRIZIONE2,
    dvr.QUANTITA,
    dvr.TUM_CODICE,
    dvr.PREZZO,
    dvr.TSM_CODICE,
    dvr.TSM_CODICE_ART,
    dvr.IMPORTO_SCONTO,
    dvr.TIPO_MOVIMENTO,
    dvr.TIV_CODICE,
    dvr.DATA_CONSEGNA,
    dvr.LOTTO,
    dvr.NOTE,
    dvr.PROGRESSIVO
FROM
    dvr
INNER JOIN
    dvt
ON
    dvr.PROGRESSIVO = dvt.PROGRESSIVO
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDRigaDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "id"),
                new ColonnaParametroTabella("IDDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "numero_documento"),
                new ColonnaParametroTabella("IDArticolo", ColonnaParametroTabella.TipiDati.Alfanumerico, "ART_CODICE"),
                new ColonnaParametroTabella("DescrizioneRiga1", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE1"),
                new ColonnaParametroTabella("DescrizioneRiga2", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE2"),
                new ColonnaParametroTabella("Quantita", ColonnaParametroTabella.TipiDati.NumeroDecimale, "QUANTITA"),
                new ColonnaParametroTabella("IDUnitaDiMisura", ColonnaParametroTabella.TipiDati.Alfanumerico, "TUM_CODICE"),
                new ColonnaParametroTabella("Prezzo", ColonnaParametroTabella.TipiDati.NumeroDecimale, "PREZZO"),
                new ColonnaParametroTabella("ScontoValore", ColonnaParametroTabella.TipiDati.Alfanumerico, "IMPORTO_SCONTO"),
                new ColonnaParametroTabella("CodiceSconto1", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE"),
                new ColonnaParametroTabella("CodiceSconto2", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE_ART"),
                new ColonnaParametroTabella("IDCausaleRigaDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIPO_MOVIMENTO"),
                new ColonnaParametroTabella("IDAliquotaIVA", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIV_CODICE"),
                new ColonnaParametroTabella("DataConsegna", ColonnaParametroTabella.TipiDati.Alfanumerico, "DATA_CONSEGNA"),
                new ColonnaParametroTabella("IDLotto", ColonnaParametroTabella.TipiDati.Alfanumerico, "LOTTO"),
                new ColonnaParametroTabella("Note", ColonnaParametroTabella.TipiDati.Alfanumerico, "NOTE"),
                new ColonnaParametroTabella("Progressivo", ColonnaParametroTabella.TipiDati.NumeroIntero, "PROGRESSIVO")
            );
            this.RegistraQuery("Fatture", @"
SELECT
    id,
    TIPO_DOCUMENTO,
    DATA_DOCUMENTO,
    numero_documento,
    CLI_CODICE,
    TVA_CODICE,
    TIV_CODICE,
    DATA_CONSEGNA,
    TPA_CODICE,
    TLV_CODICE,
    CLI_CODICE_VETTORE,
    NOTE,
    importo_acconto,
    NUMERO_COLLI,
    TSM_CODICE,
    TSM_CODICE_SCONTO,
    IMPORTO_TOTALE,
    IMPORTO_TOTALE_IVA,
    PROGRESSIVO
FROM
    fvt
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "id"),
                new ColonnaParametroTabella("IDTipoDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIPO_DOCUMENTO"),
                new ColonnaParametroTabella("DataDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "DATA_DOCUMENTO"),
                new ColonnaParametroTabella("NumeroDocumento", ColonnaParametroTabella.TipiDati.NumeroIntero, "numero_documento"),
                new ColonnaParametroTabella("IDAnagraficaIntestatario", ColonnaParametroTabella.TipiDati.Alfanumerico, "CLI_CODICE"),
                new ColonnaParametroTabella("IDValuta", ColonnaParametroTabella.TipiDati.Alfanumerico, "TVA_CODICE"),
                new ColonnaParametroTabella("IDAliquotaIva", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIV_CODICE"),
                new ColonnaParametroTabella("DataConsegna", ColonnaParametroTabella.TipiDati.Alfanumerico, "DATA_CONSEGNA"),
                new ColonnaParametroTabella("IDPagamento", ColonnaParametroTabella.TipiDati.Alfanumerico, "TPA_CODICE"),
                new ColonnaParametroTabella("IDListino", ColonnaParametroTabella.TipiDati.Alfanumerico, "TLV_CODICE"),
                new ColonnaParametroTabella("IDVettore", ColonnaParametroTabella.TipiDati.Alfanumerico, "CLI_CODICE_VETTORE"),
                new ColonnaParametroTabella("Note", ColonnaParametroTabella.TipiDati.Alfanumerico, "NOTE"),
                new ColonnaParametroTabella("Acconto", ColonnaParametroTabella.TipiDati.NumeroDecimale, "importo_acconto"),
                new ColonnaParametroTabella("Colli", ColonnaParametroTabella.TipiDati.NumeroDecimale, "NUMERO_COLLI"),
                new ColonnaParametroTabella("CodiceSconto1", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE"),
                new ColonnaParametroTabella("CodiceSconto2", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE_SCONTO"),
                new ColonnaParametroTabella("ImponibileValorizzazione", ColonnaParametroTabella.TipiDati.NumeroDecimale, "IMPORTO_TOTALE"),
                new ColonnaParametroTabella("IVAValorizzazione", ColonnaParametroTabella.TipiDati.NumeroDecimale, "IMPORTO_TOTALE_IVA"),
                new ColonnaParametroTabella("Progressivo", ColonnaParametroTabella.TipiDati.NumeroIntero, "PROGRESSIVO")
            );

            this.RegistraQuery("RigheFatture", @"
SELECT
    fvr.id,
    fvt.numero_documento,
    fvr.ART_CODICE,
    fvr.DESCRIZIONE1,
    fvr.DESCRIZIONE2,
    fvr.QUANTITA,
    fvr.TUM_CODICE,
    fvr.PREZZO,
    fvr.IMPORTO_SCONTO,
    fvr.TSM_CODICE,
    fvr.TSM_CODICE_ART,
    fvr.TIPO_MOVIMENTO,
    fvr.TIV_CODICE,
    fvr.DATA_CONSEGNA,
    fvr.LOTTO,
    fvr.NOTE,
    fvr.PROGRESSIVO
FROM
    fvr
INNER JOIN
    fvt
ON
    fvr.PROGRESSIVO = fvt.PROGRESSIVO
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDRigaDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "id"),
                new ColonnaParametroTabella("IDDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "numero_documento"),
                new ColonnaParametroTabella("IDArticolo", ColonnaParametroTabella.TipiDati.Alfanumerico, "ART_CODICE"),
                new ColonnaParametroTabella("DescrizioneRiga1", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE1"),
                new ColonnaParametroTabella("DescrizioneRiga2", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE2"),
                new ColonnaParametroTabella("Quantita", ColonnaParametroTabella.TipiDati.NumeroDecimale, "QUANTITA"),
                new ColonnaParametroTabella("IDUnitaDiMisura", ColonnaParametroTabella.TipiDati.Alfanumerico, "TUM_CODICE"),
                new ColonnaParametroTabella("Prezzo", ColonnaParametroTabella.TipiDati.NumeroDecimale, "PREZZO"),
                new ColonnaParametroTabella("ScontoValore", ColonnaParametroTabella.TipiDati.Alfanumerico, "IMPORTO_SCONTO"),
                new ColonnaParametroTabella("CodiceSconto1", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE"),
                new ColonnaParametroTabella("CodiceSconto2", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE_ART"),
                new ColonnaParametroTabella("IDCausaleRigaDocumento", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIPO_MOVIMENTO"),
                new ColonnaParametroTabella("IDAliquotaIVA", ColonnaParametroTabella.TipiDati.Alfanumerico, "TIV_CODICE"),
                new ColonnaParametroTabella("DataConsegna", ColonnaParametroTabella.TipiDati.Alfanumerico, "DATA_CONSEGNA"),
                new ColonnaParametroTabella("IDLotto", ColonnaParametroTabella.TipiDati.Alfanumerico, "LOTTO"),
                new ColonnaParametroTabella("Note", ColonnaParametroTabella.TipiDati.Alfanumerico, "NOTE"),
                new ColonnaParametroTabella("Progressivo", ColonnaParametroTabella.TipiDati.NumeroIntero, "PROGRESSIVO")
            );

            #endregion

            #region Query UnitaDiMisura

            this.RegistraQuery("UnitaDiMisura", @"
SELECT
    CODICE,
    DESCRIZIONE,
    DECIMALI
FROM
    tum
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDUnitaDiMisura", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE"),
                new ColonnaParametroTabella("DescrizioneUnitaDiMisura", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE"),
                new ColonnaParametroTabella("NumeroDecimali", ColonnaParametroTabella.TipiDati.NumeroIntero, "DECIMALI")
            );

            #endregion

            #region Query Listini

            this.RegistraQuery("Listini", @"
SELECT
    CODICE,
    DESCRIZIONE,
    IVA_INCLUSA,
    OBSOLETO
FROM
    tlv
WHERE
    OBSOLETO = 'no'
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDListino", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE"),
                new ColonnaParametroTabella("DescrizioneListino", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE"),
                new ColonnaParametroTabella("TipoValorizzazionePrezzi", ColonnaParametroTabella.TipiDati.Alfanumerico, "IVA_INCLUSA")
            );

            this.RegistraQuery("RigheListini", @"
SELECT
    lsv.id,
    TLV_CODICE,
    ART_CODICE,
    art.var_codice,
    DATA_INIZIO,
    DATA_FINE,
    art.TUM_CODICE,
    tlv.TVA_CODICE,
    PREZZO,
    lsv.NOTE,
    lsv.TSM_CODICE
FROM
    lsv
INNER JOIN
    art
ON
    lsv.ART_CODICE = art.codice
INNER JOIN
    tlv
ON
    lsv.TLV_CODICE = tlv.CODICE
",
                this.ConnessioneSmart,
                new ColonnaParametroTabella("IDListino", ColonnaParametroTabella.TipiDati.Alfanumerico, "TLV_CODICE"),
                new ColonnaParametroTabella("IDArticolo", ColonnaParametroTabella.TipiDati.Alfanumerico, "ART_CODICE"),
                new ColonnaParametroTabella("IDVarianteArticolo", ColonnaParametroTabella.TipiDati.Alfanumerico, "var_codice"),
                new ColonnaParametroTabella("DataIniziale", ColonnaParametroTabella.TipiDati.Alfanumerico, "DATA_INIZIO"),
                new ColonnaParametroTabella("DataFinale", ColonnaParametroTabella.TipiDati.Alfanumerico, "DATA_FINE"),
                new ColonnaParametroTabella("IDUnitaDiMisura", ColonnaParametroTabella.TipiDati.Alfanumerico, "TUM_CODICE"),
                new ColonnaParametroTabella("IDValuta", ColonnaParametroTabella.TipiDati.Alfanumerico, "TVA_CODICE"),
                new ColonnaParametroTabella("Prezzo", ColonnaParametroTabella.TipiDati.NumeroDecimale, "PREZZO"),
                new ColonnaParametroTabella("Note", ColonnaParametroTabella.TipiDati.Alfanumerico, "NOTE"),
                new ColonnaParametroTabella("IDRigaListino", ColonnaParametroTabella.TipiDati.NumeroIntero, "id"),
                new ColonnaParametroTabella("CodiceSconto", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE")
            );

            this.RegistraQuery("ScontiAnagrafiche", @"
SELECT
    CODICE,
    TSM_CODICE
FROM
    cli
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDAnagrafica", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE"),
                new ColonnaParametroTabella("CodiceSconto1", ColonnaParametroTabella.TipiDati.Alfanumerico, "TSM_CODICE")
            );

            #endregion

            #region Query AliquoteIva

            this.RegistraQuery("AliquoteIVA", @"
SELECT
    CODICE,
    descrizione,
    PERCENTUALE,
    OBSOLETO
FROM
    tiv
WHERE
    OBSOLETO = 'no'
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDAliquotaIVA", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE"),
                new ColonnaParametroTabella("DescrizioneAliquotaIVA", ColonnaParametroTabella.TipiDati.Alfanumerico, "descrizione"),
                new ColonnaParametroTabella("ValoreAliquotaIVA", ColonnaParametroTabella.TipiDati.NumeroDecimale, "PERCENTUALE")
            );

            #endregion

            #region Query Pagamenti

            this.RegistraQuery("Pagamenti", @"
SELECT
    CODICE,
    DESCRIZIONE
FROM
    tpa
", this.ConnessioneSmart,
                new ColonnaParametroTabella("IDPagamento", ColonnaParametroTabella.TipiDati.Alfanumerico, "CODICE"),
                new ColonnaParametroTabella("DescrizionePagamento", ColonnaParametroTabella.TipiDati.Alfanumerico, "DESCRIZIONE")
            );

            #endregion
        }

        #endregion

        #region Metodi connettore

        protected override RisultatoConDescrizione ElaborazioneAnagraficheOUT()
        {
            this.ImpostaInformazioniElaborazione("Caricamento dati anagrafiche");
            using (TabellaDati datiAnagrafiche = this.RilevaTabellaDatiDaQuery("Anagrafiche"))
            using (TabellaDati datiDestinazioniAnagrafiche = this.RilevaTabellaDatiDaQuery("DestinazioniAnagrafiche"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati anagrafiche", datiDestinazioniAnagrafiche.NumeroRighe);
                for (int i = 0; i < datiAnagrafiche.NumeroRighe; i++)
                {
                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);
                    var anagrafica = new RigaDatiAnagrafiche
                    {
                        IDAnagrafica = datiAnagrafiche[i, "IDAnagrafica"].ToTrimmedString(),
                        DescrizioneAnagrafica = datiAnagrafiche[i, "DescrizioneAnagrafica"].ToTrimmedString() + datiAnagrafiche[i, "DescrizioneAggiuntivaAnagrafica"].ToTrimmedString(),
                        Indirizzo = datiAnagrafiche[i, "Indirizzo"].ToTrimmedString(),
                        CAP = datiAnagrafiche[i, "CAP"].ToTrimmedString(),
                        Citta = datiAnagrafiche[i, "Citta"].ToTrimmedString(),
                        Stato = datiAnagrafiche[i, "Stato"].ToTrimmedString(),
                        Provincia = datiAnagrafiche[i, "Provincia"].ToTrimmedString(),
                        PartitaIVA = datiAnagrafiche[i, "PartitaIVA"].ToTrimmedString(),
                        CodiceFiscale = datiAnagrafiche[i, "CodiceFiscale"].ToTrimmedString(),
                        IDAliquotaIVA = datiAnagrafiche[i, "IDAliquotaIVA"].ToTrimmedString(),
                        Note = datiAnagrafiche[i, "Note"].ToTrimmedString(),
                        IDPagamento = datiAnagrafiche[i, "IDPagamento"].ToTrimmedString(),
                        IDListino = datiAnagrafiche[i, "IDListino"].ToTrimmedString(),
                        Fido = datiAnagrafiche[i, "Fido"].ToDecimal()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(anagrafica);

                    // Contatto anarafica

                    this.ConnessioneiSellOUT.InserisciContattoAnagraficaSeCompilato(
                        anagrafica.IDAnagrafica,
                        TipoContatto.Mail,
                        datiAnagrafiche[i, "Mail"].ToTrimmedString()
                    );

                    this.ConnessioneiSellOUT.InserisciContattoAnagraficaSeCompilato(
                        anagrafica.IDAnagrafica,
                        TipoContatto.Telefono,
                        datiAnagrafiche[i, "Telefono"].ToTrimmedString()
                    );

                    this.ConnessioneiSellOUT.InserisciContattoAnagraficaSeCompilato(
                        anagrafica.IDAnagrafica,
                        TipoContatto.Cellulare,
                        datiAnagrafiche[i, "Cellulare"].ToTrimmedString()
                    );

                    this.ConnessioneiSellOUT.InserisciContattoAnagraficaSeCompilato(
                        anagrafica.IDAnagrafica,
                        TipoContatto.Fax,
                        datiAnagrafiche[i, "Fax"].ToTrimmedString()
                    );

                    this.ConnessioneiSellOUT.InserisciContattoAnagraficaSeCompilato(
                        anagrafica.IDAnagrafica,
                        TipoContatto.SitoInternet,
                        datiAnagrafiche[i, "SitoInternet"].ToTrimmedString()
                    );

                    this.ConnessioneiSellOUT.InserisciContattoAnagraficaSeCompilato(
                        anagrafica.IDAnagrafica,
                        TipoContatto.PEC,
                        datiAnagrafiche[i, "PEC"].ToTrimmedString()
                    );
                }

                // Relazioni Anagrafiche

                {
                    for (int i = 0; i < datiDestinazioniAnagrafiche.NumeroRighe; i++)
                    {
                        var relazioniAnagrafiche = new RigaDatiRelazioniAnagrafiche
                        {
                            IDAnagrafica = datiDestinazioniAnagrafiche[i, "IDAnagraficaSuperiore"].ToTrimmedString() + "_" + datiDestinazioniAnagrafiche[i, "IDAnagraficaDestinazione"].ToTrimmedString(),
                            IDAnagraficaSuperiore = datiDestinazioniAnagrafiche[i, "IDAnagraficaSuperiore"].ToTrimmedString(),
                            TipoRelazione = TipoRelazione.Destinatario
                        };
                        this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(relazioniAnagrafiche);

                        // Destinazioni alternative anagrafiche

                        var destinazioniAlternativeAnagrafiche = new RigaDatiAnagrafiche
                        {
                            IDAnagrafica = datiDestinazioniAnagrafiche[i, "IDAnagraficaSuperiore"].ToTrimmedString() + "_" + datiDestinazioniAnagrafiche[i, "IDAnagraficaDestinazione"].ToTrimmedString(),
                            DescrizioneAnagrafica = datiDestinazioniAnagrafiche[i, "DescrizioneAnagrafica"].ToTrimmedString() + datiDestinazioniAnagrafiche[i, "DescrizioneAggiuntivaAnagrafica"].ToTrimmedString(),
                            Indirizzo = datiDestinazioniAnagrafiche[i, "Indirizzo"].ToTrimmedString(),
                            CAP = datiDestinazioniAnagrafiche[i, "CAP"].ToTrimmedString(),
                            Citta = datiDestinazioniAnagrafiche[i, "Citta"].ToTrimmedString(),
                            Provincia = datiDestinazioniAnagrafiche[i, "Provincia"].ToTrimmedString(),
                            Note = datiDestinazioniAnagrafiche[i, "Note"].ToTrimmedString()
                        };
                        this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(destinazioniAlternativeAnagrafiche);
                    }
                }
            }

            // Operatori

            this.ImpostaInformazioniElaborazione("Caricamento dati operatori");
            using (TabellaDati datiOperatori = this.RilevaTabellaDatiDaQuery("Operatori"))
            {
                for (int i = 0; i < datiOperatori.NumeroRighe; i++)
                {
                    var operatore = new RigaDatiOperatori
                    {
                        IDOperatore = datiOperatori[i, "IDOperatore"].ToTrimmedString(),
                        DescrizioneOperatore = datiOperatori[i, "DescrizioneOperatore"].ToTrimmedString()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(operatore);
                }
            }

            // Aliquote IVA

            this.ImpostaInformazioniElaborazione("Caricamento dati aliquote IVA");
            using (TabellaDati datiAliquoteIVA = this.RilevaTabellaDatiDaQuery("AliquoteIVA"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati aliquote IVA", datiAliquoteIVA.NumeroRighe);
                for (int i = 0; i < datiAliquoteIVA.NumeroRighe; i++)
                {
                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);
                    var aliquotaIVA = new RigaDatiAliquoteIVA
                    {
                        IDAliquotaIVA = datiAliquoteIVA[i, "IDAliquotaIVA"].ToTrimmedString(),
                        DescrizioneAliquotaIVA = datiAliquoteIVA[i, "DescrizioneAliquotaIVA"].ToTrimmedString(),
                        ValoreAliquotaIVA = datiAliquoteIVA[i, "ValoreAliquotaIVA"].ToDecimal()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(aliquotaIVA);
                }
            }

            // Pagamenti

            this.ImpostaInformazioniElaborazione("Caricamento dati pagamenti");
            using (TabellaDati datiPagamenti = this.RilevaTabellaDatiDaQuery("Pagamenti"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati pagamenti", datiPagamenti.NumeroRighe);
                for (int i = 0; i < datiPagamenti.NumeroRighe; i++)
                {
                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);
                    var pagamento = new RigaDatiPagamenti
                    {
                        IDPagamento = datiPagamenti[i, "IDPagamento"].ToTrimmedString(),
                        DescrizionePagamento = datiPagamenti[i, "DescrizionePagamento"].ToTrimmedString()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(pagamento);
                }
            }

            return new RisultatoConDescrizione(true);
        }

        protected override RisultatoConDescrizione ElaborazioneArticoliOUT()
        {
            this.ImpostaInformazioniElaborazione("Caricamento dati articoli");
            using (TabellaDati datiArticoli = this.RilevaTabellaDatiDaQuery("Articoli"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati articoli", datiArticoli.NumeroRighe);

                for (int i = 0; i < datiArticoli.NumeroRighe; i++)
                {
                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);
                    var articolo = new RigaDatiArticoli()
                    {
                        IDArticolo = datiArticoli[i, "IDArticolo"].ToTrimmedString(),
                        DescrizioneArticolo = datiArticoli[i, "DescrizioneArticolo"].ToTrimmedString() + datiArticoli[i, "DescrizioneAggiuntivaArticolo"].ToTrimmedString(),
                        IDUnitaDiMisura = datiArticoli[i, "IDUnitaDiMisura"].ToTrimmedString(),
                        IDAliquotaIVA = datiArticoli[i, "IDAliquotaIVA"].ToTrimmedString(),
                        GestioneLotti = datiArticoli[i, "GestioneLotti"].ToTrimmedString().UgualeCaseInsensitive("si") ? GestioneLotti.Gestito : GestioneLotti.NonGestito,
                        Note = datiArticoli[i, "Note"].ToTrimmedString(),
                        IDTipoArticolo = datiArticoli[i, "IDTipoArticolo"].ToTrimmedString(),
                        IDTipoVariantiArticoli = datiArticoli[i, "IDTipoVariantiArticoli"].ToTrimmedString()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(articolo);

                    // Articoli in gruppi articoli                    

                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(new RigaDatiArticoliInGruppiArticoli
                    {
                        IDArticolo = articolo.IDArticolo,
                        IDGruppoArticoli = PREFISSO_GRUPPI_ARTICOLI_MARCHE + datiArticoli[i, "IDGruppoArticoliMarche"].ToTrimmedString()
                    });

                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(new RigaDatiArticoliInGruppiArticoli
                    {
                        IDArticolo = articolo.IDArticolo,
                        IDGruppoArticoli = PREFISSO_GRUPPI_ARTICOLI_CATEGORIE_MERCEOLOGICHE + datiArticoli[i, "IDGruppoArticoliCategorieMerceologiche"].ToTrimmedString()
                    });
                }
            }

            //Gruppi articoli

            this.ImpostaInformazioniElaborazione("Caricamento dati gruppi articoli");
            using (TabellaDati datiCategorieArticoli = this.RilevaTabellaDatiDaQuery("CategorieArticoli"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati gruppi articoli", datiCategorieArticoli.NumeroRighe);
                int valoreParametro = this.RilevaValoreParametro("GruppiMerceologici").ToInt();

                var gruppiArticoliSuperiori = new RigaDatiGruppiArticoli
                {
                    IDGruppoArticoli = ID_GRUPPO_ARTICOLI_SUPERIORE_GRUPPO_MERCEOLOGICO,
                    DescrizioneGruppoArticoli = ID_GRUPPO_ARTICOLI_SUPERIORE_GRUPPO_MERCEOLOGICO
                };
                this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(gruppiArticoliSuperiori);

                for (int i = 0; i < datiCategorieArticoli.NumeroRighe; i++)
                {
                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);

                    var gruppiArticoli = new RigaDatiGruppiArticoli()
                    {
                        IDGruppoArticoli = PREFISSO_GRUPPI_ARTICOLI_CATEGORIE_MERCEOLOGICHE + datiCategorieArticoli[i, "IDGruppoArticoli"].ToTrimmedString(),
                        DescrizioneGruppoArticoli = datiCategorieArticoli[i, "DescrizioneGruppoArticoli"].ToTrimmedString(),
                    };
                    if (valoreParametro == 1)
                    {
                        gruppiArticoli.IDGruppoArticoliSuperiore = ID_GRUPPO_ARTICOLI_SUPERIORE_GRUPPO_MERCEOLOGICO;

                        this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(gruppiArticoli);
                    }
                    else if (valoreParametro == 2)
                    {
                        gruppiArticoli.IDGruppoArticoliSuperiore = "";

                        this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(gruppiArticoli);
                    }
                    else
                    {
                        gruppiArticoli.IDGruppoArticoliSuperiore = "";

                        gruppiArticoli.TipoGruppoArticoli = TipoGruppoArticoli.GestioneInterna;
                        this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(gruppiArticoli);
                    }
                }
            }

            this.ImpostaInformazioniElaborazione("Caricamento dati marche articoli");
            using (TabellaDati datiMarcheArticoli = this.RilevaTabellaDatiDaQuery("MarcheArticoli"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati marche articoli", datiMarcheArticoli.NumeroRighe);
                int valoreParametro = this.RilevaValoreParametro("GruppiMarche").ToInt();

                var gruppiArticoliSuperiori = new RigaDatiGruppiArticoli
                {
                    IDGruppoArticoli = ID_GRUPPO_ARTICOLI_SUPERIORE_MARCHE,
                    DescrizioneGruppoArticoli = ID_GRUPPO_ARTICOLI_SUPERIORE_MARCHE
                };
                this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(gruppiArticoliSuperiori);

                for (int i = 0; i < datiMarcheArticoli.NumeroRighe; i++)
                {
                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);

                    var gruppiArticoli = new RigaDatiGruppiArticoli()
                    {
                        IDGruppoArticoli = PREFISSO_GRUPPI_ARTICOLI_MARCHE + datiMarcheArticoli[i, "IDGruppoArticoli"].ToTrimmedString(),
                        DescrizioneGruppoArticoli = datiMarcheArticoli[i, "DescrizioneGruppoArticoli"].ToTrimmedString()
                    };
                    if (valoreParametro == 1)
                    {
                        gruppiArticoli.IDGruppoArticoliSuperiore = ID_GRUPPO_ARTICOLI_SUPERIORE_MARCHE;

                        this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(gruppiArticoli);
                    }
                    else if (valoreParametro == 2)
                    {
                        gruppiArticoli.IDGruppoArticoliSuperiore = "";

                        this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(gruppiArticoli);
                    }
                    else
                    {
                        gruppiArticoli.IDGruppoArticoliSuperiore = "";

                        gruppiArticoli.TipoGruppoArticoli = TipoGruppoArticoli.GestioneInterna;
                        this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(gruppiArticoli);
                    }
                }
            }

            //Unità di misura

            this.ImpostaInformazioniElaborazione("Caricamento dati unità di misura");
            using (TabellaDati datiUnitaDiMisura = this.RilevaTabellaDatiDaQuery("UnitaDiMisura"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati unità di misura", datiUnitaDiMisura.NumeroRighe);
                for (int i = 0; i < datiUnitaDiMisura.NumeroRighe; i++)
                {
                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);
                    var unitaDiMisura = new RigaDatiUnitaDiMisura
                    {
                        IDUnitaDiMisura = datiUnitaDiMisura[i, "IDUnitaDiMisura"].ToTrimmedString(),
                        DescrizioneUnitaDiMisura = datiUnitaDiMisura[i, "DescrizioneUnitaDiMisura"].ToTrimmedString().UgualeCaseInsensitive("") ? datiUnitaDiMisura[i, "IDUnitaDiMisura"].ToTrimmedString() : datiUnitaDiMisura[i, "DescrizioneUnitaDiMisura"].ToTrimmedString(),
                        NumeroDecimali = datiUnitaDiMisura[i, "NumeroDecimali"].ToInt()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(unitaDiMisura);
                }
            }

            return new RisultatoConDescrizione(true);
        }

        protected override RisultatoConDescrizione ElaborazioneListiniOUT()
        {
            //Sconto valore non gestito
            this.ConnessioneiSellOUT.AccodaRegistrazioneImpostazioneApplicazioneISell(
                "ModificaScontoValoreRigaDocumento",
                "",
                "0"
            );

            bool consentiModificaSconti = this.RilevaValoreParametro("ConsentiModificaSconti").ToBool();
            if (consentiModificaSconti == true)
            {
                return new RisultatoConDescrizione(false, "Modifica sconti attualmente non implementata. Manca la generazione di nuovi codici nella tsm");
            }
            else
            {
                //Non può modificare gli sconti delle righe documenti
                this.ConnessioneiSellOUT.AccodaRegistrazioneImpostazioneApplicazioneISell(
                    "ModificaScontiRigaDocumento",
                    "",
                    "2");
                //Non può modificare gli sconti di chiusura dei documenti
                this.ConnessioneiSellOUT.AccodaRegistrazioneImpostazioneApplicazioneISell(
                    "ModificaScontiChiusuraDocumento",
                    "",
                    "2");
                //Il numero di sconti di chiusura dei documenti è 0
                this.ConnessioneiSellOUT.AccodaRegistrazioneImpostazioneApplicazioneISell(
                    "NumeroScontiChiusuraDocumento",
                    "",
                    "0");
            }

            this.ImpostaInformazioniElaborazione("Caricamento dati listini");
            using (TabellaDati datiListini = this.RilevaTabellaDatiDaQuery("Listini"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati listini", datiListini.NumeroRighe);
                for (int i = 0; i < datiListini.NumeroRighe; i++)
                {
                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);

                    var listini = new RigaDatiListini
                    {
                        IDListino = datiListini[i, "IDListino"].ToTrimmedString(),
                        DescrizioneListino = datiListini[i, "DescrizioneListino"].ToTrimmedString(),
                        TipoValorizzazionePrezzi = datiListini[i, "TipoValorizzazionePrezzi"].ToTrimmedString().UgualeCaseInsensitive("si") ? TipoValorizzazionePrezzi.PrezzoIvato : TipoValorizzazionePrezzi.PrezzoNonIvato
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(listini);
                }
            }

            // Righe listini

            this.ImpostaInformazioniElaborazione("Caricamento righe listini");
            var dizionarioTabellaTsm = this.PrelevaListaSconti();
            using (TabellaDati datiRigheListini = this.RilevaTabellaDatiDaQuery("RigheListini"))
            {
                var listaScontiRigaListino = new List<decimal>();
                this.ImpostaInformazioniElaborazione("Caricamento righe listini", datiRigheListini.NumeroRighe);
                for (int i = 0; i < datiRigheListini.NumeroRighe; i++)
                {
                    listaScontiRigaListino.Clear();
                    // Gestisce codici sconto per righe listini
                    if (dizionarioTabellaTsm.TryGetValue(datiRigheListini[i, "CodiceSconto"].ToTrimmedString(), out var listaScontiRilevati1))
                        listaScontiRigaListino.AddRange(listaScontiRilevati1);

                    string idRigaListino = datiRigheListini[i, "IDRigaListino"].ToTrimmedString();
                    if (listaScontiRigaListino.Count > 8)
                        this.RegistraLog($"Numero sconti maggiore di 8 per riga listino. IDRigaListino: {idRigaListino}", TipiLog.Avviso);

                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);
                    var righeListini = new RigaDatiRigheListini
                    {
                        IDListino = datiRigheListini[i, "IDListino"].ToTrimmedString(),
                        IDArticolo = datiRigheListini[i, "IDArticolo"].ToTrimmedString(),
                        IDVarianteArticolo = datiRigheListini[i, "IDVarianteArticolo"].ToTrimmedString(),
                        DataIniziale = datiRigheListini[i, "DataIniziale"].ToTrimmedString(),
                        DataFinale = datiRigheListini[i, "DataFinale"].ToTrimmedString(),
                        IDUnitaDiMisura = datiRigheListini[i, "IDUnitaDiMisura"].ToTrimmedString(),
                        IDValuta = datiRigheListini[i, "IDValuta"].ToTrimmedString(),
                        Prezzo = datiRigheListini[i, "Prezzo"].ToDecimal(),
                        Sconto1 = listaScontiRigaListino.Count > 0 ? listaScontiRigaListino[0] : 0,
                        Sconto2 = listaScontiRigaListino.Count > 1 ? listaScontiRigaListino[1] : 0,
                        Sconto3 = listaScontiRigaListino.Count > 2 ? listaScontiRigaListino[2] : 0,
                        Sconto4 = listaScontiRigaListino.Count > 3 ? listaScontiRigaListino[3] : 0,
                        Sconto5 = listaScontiRigaListino.Count > 4 ? listaScontiRigaListino[4] : 0,
                        Sconto6 = listaScontiRigaListino.Count > 5 ? listaScontiRigaListino[5] : 0,
                        Sconto7 = listaScontiRigaListino.Count > 6 ? listaScontiRigaListino[6] : 0,
                        Sconto8 = listaScontiRigaListino.Count > 7 ? listaScontiRigaListino[7] : 0,
                        Note = datiRigheListini[i, "Note"].ToTrimmedString()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(righeListini);
                }
            }

            // Sconti per anagrafiche

            using (TabellaDati datiScontiAnagrafiche = this.RilevaTabellaDatiDaQuery("ScontiAnagrafiche"))
            {
                List<decimal> listaScontiAnagrafica = new List<decimal>();
                for (int i = 0; i < datiScontiAnagrafiche.NumeroRighe; i++)
                {
                    listaScontiAnagrafica.Clear();
                    // Gestisce sconto 
                    if (dizionarioTabellaTsm.TryGetValue(datiScontiAnagrafiche[i, "CodiceSconto1"].ToTrimmedString(), out var listaScontiRilevati1))
                        listaScontiAnagrafica.AddRange(listaScontiRilevati1);

                    string idAnagrafica = datiScontiAnagrafiche[i, "IDAnagrafica"].ToTrimmedString();
                    if (listaScontiAnagrafica.Count > 8)
                        this.RegistraLog($"Numero sconti maggiore di 8 per anagrafica. IdAnagrafica: {idAnagrafica}", TipiLog.Avviso);

                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);

                    // Gestisce sconti
                    string stringaScontiAnagrafiche = String.Join(";", listaScontiAnagrafica);

                    this.ConnessioneiSellOUT.AccodaRegistrazioneImpostazioneApplicazioneISell(
                        "ValoriScontiRigaDocumento",
                        "IDAnagrafica=" + datiScontiAnagrafiche[i, "IDAnagrafica"].ToTrimmedString(),
                        // Concatenare valori sconto come stringa
                        stringaScontiAnagrafiche
                    );
                }
            }

            return new RisultatoConDescrizione(true);
        }

        protected override RisultatoConDescrizione ElaborazioneDocumentiOUT()
        {
            this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(new RigaDatiCausaliRigheDocumenti
            {
                IDCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_OMAGGIO_TOTALE,
                DescrizioneCausaleRigaDocumento = "Omaggio totale",
                MovimentazioneOmaggi = MovimentazioneOmaggi.Positivo,
                IdentificatoreVisualeTipoCausale = IdentificatoreVisuale.Giallo
            });

            this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(new RigaDatiCausaliRigheDocumenti
            {
                IDCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_OMAGGIO_IMPONIBILE,
                DescrizioneCausaleRigaDocumento = "Omaggio imponibile",
                MovimentazioneOmaggi = MovimentazioneOmaggi.Positivo,
                MovimentazioneIVA = MovimentazioneIVA.Positivo,
                IdentificatoreVisualeTipoCausale = IdentificatoreVisuale.Giallo
            });

            this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(new RigaDatiCausaliRigheDocumenti
            {
                IDCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_SCONTO_MERCE,
                DescrizioneCausaleRigaDocumento = "Sconto merce",
                MovimentazioneOmaggi = MovimentazioneOmaggi.Positivo
            });

            this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(new RigaDatiCausaliRigheDocumenti
            {
                IDCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_VENDITA,
                DescrizioneCausaleRigaDocumento = "Vendita",
                MovimentazioneImponibile = MovimentazioneImponibile.Positivo,
                MovimentazioneIVA = MovimentazioneIVA.Positivo,
                IdentificatoreVisualeTipoCausale = IdentificatoreVisuale.Verde
            });

            var dizionarioTabellaTsm = this.PrelevaListaSconti();
            this.ImpostaInformazioniElaborazione("Caricamento dati fatture");
            using (TabellaDati datiTestataFatture = this.RilevaTabellaDatiDaQuery("Fatture"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati fatture", datiTestataFatture.NumeroRighe);
                List<decimal> listaScontiTestataDocumento = new List<decimal>();
                for (int i = 0; i < datiTestataFatture.NumeroRighe; i++)
                {
                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);

                    listaScontiTestataDocumento.Clear();
                    // Gestisce sconto 
                    if (dizionarioTabellaTsm.TryGetValue(datiTestataFatture[i, "CodiceSconto1"].ToTrimmedString(), out var listaScontiRilevati1))
                        listaScontiTestataDocumento.AddRange(listaScontiRilevati1);

                    if (dizionarioTabellaTsm.TryGetValue(datiTestataFatture[i, "CodiceSconto2"].ToTrimmedString(), out var listaScontiRilevati2))
                        listaScontiTestataDocumento.AddRange(listaScontiRilevati2);

                    string idDocumento = datiTestataFatture[i, "IDDocumento"].ToTrimmedString();
                    if (listaScontiTestataDocumento.Count > 8)
                        this.RegistraLog($"Numero sconti maggiore di 8 per testata fattura. IDDocumento: {idDocumento}", TipiLog.Avviso);

                    string idTipoDocumento = "";
                    switch (datiTestataFatture[i, "IDTipoDocumento"].ToTrimmedString())
                    {
                        case "fattura accompagnatoria":
                            idTipoDocumento = ID_TIPO_DOCUMENTO_FATTURA_ACCOMPAGNATORIA;
                            break;
                        case "fattura differita":
                            idTipoDocumento = ID_TIPO_DOCUMENTO_FATTURA_DIFFERITA;
                            break;
                        case "fattura immediata":
                            idTipoDocumento = ID_TIPO_DOCUMENTO_FATTURA_IMMEDIATA;
                            break;
                        case "nota credito":
                            idTipoDocumento = ID_TIPO_DOCUMENTO_NOTA_CREDITO;
                            break;
                    }

                    var testataFatture = new RigaDatiDocumenti
                    {
                        IDDocumento = "FATT_" + datiTestataFatture[i, "NumeroDocumento"].ToInt() + "_" + datiTestataFatture[i, "Progressivo"].ToInt(),
                        IDTipoDocumento = idTipoDocumento,
                        DataDocumento = datiTestataFatture[i, "DataDocumento"].ToTrimmedString(),
                        NumeroDocumento = datiTestataFatture[i, "NumeroDocumento"].ToInt(),
                        IDAnagraficaIntestatario = datiTestataFatture[i, "IDAnagraficaIntestatario"].ToTrimmedString(),
                        IDValuta = datiTestataFatture[i, "IDValuta"].ToTrimmedString(),
                        IDAliquotaIVA = datiTestataFatture[i, "IDAliquotaIVA"].ToTrimmedString(),
                        DataConsegna = datiTestataFatture[i, "DataConsegna"].ToTrimmedString(),
                        IDPagamento = datiTestataFatture[i, "IDPagamento"].ToTrimmedString(),
                        IDListino = datiTestataFatture[i, "IDListino"].ToTrimmedString(),
                        IDVettore = datiTestataFatture[i, "IDVettore"].ToTrimmedString(),
                        Note = datiTestataFatture[i, "Note"].ToTrimmedString(),
                        Acconto = datiTestataFatture[i, "Acconto"].ToDecimal(),
                        Colli = datiTestataFatture[i, "Colli"].ToDecimal(),
                        ScontoChiusura1 = listaScontiTestataDocumento.Count > 0 ? listaScontiTestataDocumento[0] : 0,
                        ScontoChiusura2 = listaScontiTestataDocumento.Count > 1 ? listaScontiTestataDocumento[1] : 0,
                        ScontoChiusura3 = listaScontiTestataDocumento.Count > 2 ? listaScontiTestataDocumento[2] : 0,
                        ImponibileValorizzazione = datiTestataFatture[i, "ImponibileValorizzazione"].ToDecimal(),
                        IVAValorizzazione = datiTestataFatture[i, "IVAValorizzazione"].ToDecimal(),
                        TotaleValorizzazione = datiTestataFatture[i, "ImponibileValorizzazione"].ToDecimal() + datiTestataFatture[i, "IVAValorizzazione"].ToDecimal()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(testataFatture);
                }
            }

            this.ImpostaInformazioniElaborazione("Caricamento dati righe fatture");
            using (TabellaDati datiRigheFatture = this.RilevaTabellaDatiDaQuery("RigheFatture"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati righe fatture", datiRigheFatture.NumeroRighe);
                List<decimal> listaScontiRigaDocumento = new List<decimal>();
                for (int i = 0; i < datiRigheFatture.NumeroRighe; i++)
                {
                    listaScontiRigaDocumento.Clear();
                    this.AvanzaStatoElaborazione();
                    // Gestisce sconto
                    if (dizionarioTabellaTsm.TryGetValue(datiRigheFatture[i, "CodiceSconto1"].ToTrimmedString(), out var listaScontiRilevati1))
                        listaScontiRigaDocumento.AddRange(listaScontiRilevati1);

                    if (dizionarioTabellaTsm.TryGetValue(datiRigheFatture[i, "CodiceSconto2"].ToTrimmedString(), out var listaScontiRilevati2))
                        listaScontiRigaDocumento.AddRange(listaScontiRilevati2);

                    string idRigaDocumento = datiRigheFatture[i, "IDRigaDocumento"].ToTrimmedString();
                    if (listaScontiRigaDocumento.Count > 8)
                        this.RegistraLog($"Numero sconti maggiore di 8 per riga fattura. IDRigaDocumento: {idRigaDocumento}", TipiLog.Avviso);

                    string idCausaleRigaDocumento = "";
                    switch (datiRigheFatture[i, "IDCausaleRigaDocumento"].ToTrimmedString())
                    {
                        case "normale":
                            idCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_VENDITA;
                            break;
                        case "omaggio":
                            idCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_OMAGGIO_IMPONIBILE;
                            break;
                        case "omaggio no rivalsa":
                            idCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_OMAGGIO_TOTALE;
                            break;
                        case "sconto merce":
                            idCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_SCONTO_MERCE;
                            break;
                    }

                    var righeFatture = new RigaDatiRigheDocumenti
                    {
                        IDRigaDocumento = "FATT_" + datiRigheFatture[i, "IDDocumento"].ToTrimmedString() + "_" + idRigaDocumento,
                        IDDocumento = "FATT_" + datiRigheFatture[i, "IDDocumento"].ToInt() + "_" + datiRigheFatture[i, "Progressivo"].ToInt(),
                        IDArticolo = datiRigheFatture[i, "IDArticolo"].ToTrimmedString(),
                        DescrizioneRiga = datiRigheFatture[i, "DescrizioneRiga1"].ToTrimmedString() + datiRigheFatture[i, "DescrizioneRiga2"].ToTrimmedString(),
                        Quantita = datiRigheFatture[i, "Quantita"].ToDecimal(),
                        IDUnitaDiMisura = datiRigheFatture[i, "IDUnitaDiMisura"].ToTrimmedString(),
                        IDCausaleRigaDocumento = idCausaleRigaDocumento,
                        Prezzo = datiRigheFatture[i, "Prezzo"].ToDecimal(),
                        // Gestisce sconto
                        Sconto1 = listaScontiRigaDocumento.Count > 0 ? listaScontiRigaDocumento[0] : 0,
                        Sconto2 = listaScontiRigaDocumento.Count > 1 ? listaScontiRigaDocumento[1] : 0,
                        Sconto3 = listaScontiRigaDocumento.Count > 2 ? listaScontiRigaDocumento[2] : 0,
                        Sconto4 = listaScontiRigaDocumento.Count > 3 ? listaScontiRigaDocumento[3] : 0,
                        Sconto5 = listaScontiRigaDocumento.Count > 4 ? listaScontiRigaDocumento[4] : 0,
                        Sconto6 = listaScontiRigaDocumento.Count > 5 ? listaScontiRigaDocumento[5] : 0,
                        Sconto7 = listaScontiRigaDocumento.Count > 6 ? listaScontiRigaDocumento[6] : 0,
                        Sconto8 = listaScontiRigaDocumento.Count > 7 ? listaScontiRigaDocumento[7] : 0,
                        IDAliquotaIVA = datiRigheFatture[i, "IDAliquotaIVA"].ToTrimmedString(),
                        DataConsegna = datiRigheFatture[i, "DataConsegna"].ToTrimmedString(),
                        IDLotto = datiRigheFatture[i, "IDLotto"].ToTrimmedString(),
                        Note = datiRigheFatture[i, "Note"].ToTrimmedString()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(righeFatture);
                }
            }

            this.ImpostaInformazioniElaborazione("Caricamento dati ddt");
            using (TabellaDati datiTestataDdt = this.RilevaTabellaDatiDaQuery("Ddt"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati ddt", datiTestataDdt.NumeroRighe);
                List<decimal> listaScontiTestataDocumento = new List<decimal>();
                for (int i = 0; i < datiTestataDdt.NumeroRighe; i++)
                {
                    listaScontiTestataDocumento.Clear();
                    // Gestisce sconto 
                    if (dizionarioTabellaTsm.TryGetValue(datiTestataDdt[i, "CodiceSconto1"].ToTrimmedString(), out var listaScontiRilevati1))
                    {
                        listaScontiTestataDocumento.AddRange(listaScontiRilevati1);
                    }

                    if (dizionarioTabellaTsm.TryGetValue(datiTestataDdt[i, "CodiceSconto2"].ToTrimmedString(), out var listaScontiRilevati2))
                    {
                        listaScontiTestataDocumento.AddRange(listaScontiRilevati2);
                    }

                    string idDocumento = datiTestataDdt[i, "IDDocumento"].ToTrimmedString();
                    if (listaScontiTestataDocumento.Count > 8)
                        this.RegistraLog($"Numero sconti maggiore di 8 per testata fattura. IDDocumento: {idDocumento}", TipiLog.Avviso);

                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);

                    string idTipoDocumento = "";
                    switch (datiTestataDdt[i, "IDTipoDocumento"].ToTrimmedString())
                    {
                        case "ddt":
                            idTipoDocumento = ID_TIPO_DOCUMENTO_DDT;
                            break;
                        case "ddt fornitori":
                            idTipoDocumento = ID_TIPO_DOCUMENTO_DDT_FORNITORI;
                            break;
                    }

                    var testataDdt = new RigaDatiDocumenti
                    {
                        IDDocumento = "DDT_" + datiTestataDdt[i, "NumeroDocumento"] + "_" + datiTestataDdt[i, "Progressivo"].ToInt(),
                        IDTipoDocumento = idTipoDocumento,
                        DataDocumento = datiTestataDdt[i, "DataDocumento"].ToTrimmedString(),
                        NumeroDocumento = datiTestataDdt[i, "NumeroDocumento"].ToInt(),
                        IDAnagraficaIntestatario = datiTestataDdt[i, "IDAnagraficaIntestatario"].ToTrimmedString(),
                        IDValuta = datiTestataDdt[i, "IDValuta"].ToTrimmedString(),
                        IDAliquotaIVA = datiTestataDdt[i, "IDAliquotaIVA"].ToTrimmedString(),
                        DataConsegna = datiTestataDdt[i, "DataConsegna"].ToTrimmedString(),
                        IDPagamento = datiTestataDdt[i, "IDPagamento"].ToTrimmedString(),
                        IDListino = datiTestataDdt[i, "IDListino"].ToTrimmedString(),
                        IDVettore = datiTestataDdt[i, "IDVettore"].ToTrimmedString(),
                        Note = datiTestataDdt[i, "Note"].ToTrimmedString(),
                        Acconto = datiTestataDdt[i, "Acconto"].ToDecimal(),
                        Colli = datiTestataDdt[i, "Colli"].ToDecimal(),
                        ScontoChiusura1 = listaScontiTestataDocumento.Count > 0 ? listaScontiTestataDocumento[0] : 0,
                        ScontoChiusura2 = listaScontiTestataDocumento.Count > 1 ? listaScontiTestataDocumento[1] : 0,
                        ScontoChiusura3 = listaScontiTestataDocumento.Count > 2 ? listaScontiTestataDocumento[2] : 0,
                        ImponibileValorizzazione = datiTestataDdt[i, "ImponibileValorizzazione"].ToDecimal(),
                        IVAValorizzazione = datiTestataDdt[i, "IVAValorizzazione"].ToDecimal(),
                        TotaleValorizzazione = datiTestataDdt[i, "ImponibileValorizzazione"].ToDecimal() + datiTestataDdt[i, "IVAValorizzazione"].ToDecimal()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(testataDdt);
                }
            }

            this.ImpostaInformazioniElaborazione("Caricamento dati righe ddt");
            using (TabellaDati datiRigheDdt = this.RilevaTabellaDatiDaQuery("RigheDdt"))
            {
                this.ImpostaInformazioniElaborazione("Caricamento dati righe ddt", datiRigheDdt.NumeroRighe);
                List<decimal> listaScontiRigaDocumento = new List<decimal>();
                for (int i = 0; i < datiRigheDdt.NumeroRighe; i++)
                {
                    this.AvanzaStatoElaborazione();

                    if (this.InterruzioneElaborazioneInCorso)
                        return new RisultatoConDescrizione(true);

                    listaScontiRigaDocumento.Clear();
                    // Gestisce sconto
                    if (dizionarioTabellaTsm.TryGetValue(datiRigheDdt[i, "CodiceSconto1"].ToTrimmedString(), out var listaScontiRilevati1))
                    {
                        var codiceScontoTest = datiRigheDdt[i, "CodiceSconto1"].ToTrimmedString();
                        listaScontiRigaDocumento.AddRange(listaScontiRilevati1);
                    }

                    if (dizionarioTabellaTsm.TryGetValue(datiRigheDdt[i, "CodiceSconto2"].ToTrimmedString(), out var listaScontiRilevati2))
                    {
                        listaScontiRigaDocumento.AddRange(listaScontiRilevati2);
                    }

                    string idRigaDocumento = datiRigheDdt[i, "IDRigaDocumento"].ToTrimmedString();
                    if (listaScontiRigaDocumento.Count > 8)
                        this.RegistraLog($"Numero sconti maggiore di 8 per riga fattura. IDRigaDocumento: {idRigaDocumento}", TipiLog.Avviso);
                    string idCausaleRigaDocumento = "";
                    switch (datiRigheDdt[i, "IDCausaleRigaDocumento"].ToTrimmedString())
                    {
                        case "normale":
                            idCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_VENDITA;
                            break;
                        case "omaggio":
                            idCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_OMAGGIO_IMPONIBILE;
                            break;
                        case "omaggio no rivalsa":
                            idCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_OMAGGIO_TOTALE;
                            break;
                        case "sconto merce":
                            idCausaleRigaDocumento = ID_CAUSALE_RIGA_DOCUMENTO_SCONTO_MERCE;
                            break;
                    }

                    var righeDdt = new RigaDatiRigheDocumenti
                    {
                        IDRigaDocumento = "DDT_" + datiRigheDdt[i, "IDDocumento"].ToTrimmedString() + "_" + idRigaDocumento,
                        IDDocumento = "DDT_" + datiRigheDdt[i, "IDDocumento"].ToTrimmedString() + "_" + datiRigheDdt[i, "Progressivo"].ToInt(),
                        IDArticolo = datiRigheDdt[i, "IDArticolo"].ToTrimmedString(),
                        DescrizioneRiga = datiRigheDdt[i, "DescrizioneRiga1"].ToTrimmedString() + datiRigheDdt[i, "DescrizioneRiga2"].ToTrimmedString(),
                        Quantita = datiRigheDdt[i, "Quantita"].ToDecimal(),
                        IDUnitaDiMisura = datiRigheDdt[i, "IDUnitaDiMisura"].ToTrimmedString(),
                        IDCausaleRigaDocumento = idCausaleRigaDocumento,
                        Prezzo = datiRigheDdt[i, "Prezzo"].ToDecimal(),
                        // Gestisce sconto
                        Sconto1 = listaScontiRigaDocumento.Count > 0 ? listaScontiRigaDocumento[0] : 0,
                        Sconto2 = listaScontiRigaDocumento.Count > 1 ? listaScontiRigaDocumento[1] : 0,
                        Sconto3 = listaScontiRigaDocumento.Count > 2 ? listaScontiRigaDocumento[2] : 0,
                        Sconto4 = listaScontiRigaDocumento.Count > 3 ? listaScontiRigaDocumento[3] : 0,
                        Sconto5 = listaScontiRigaDocumento.Count > 4 ? listaScontiRigaDocumento[4] : 0,
                        Sconto6 = listaScontiRigaDocumento.Count > 5 ? listaScontiRigaDocumento[5] : 0,
                        Sconto7 = listaScontiRigaDocumento.Count > 6 ? listaScontiRigaDocumento[6] : 0,
                        Sconto8 = listaScontiRigaDocumento.Count > 7 ? listaScontiRigaDocumento[7] : 0,
                        IDAliquotaIVA = datiRigheDdt[i, "IDAliquotaIVA"].ToTrimmedString(),
                        DataConsegna = datiRigheDdt[i, "DataConsegna"].ToTrimmedString(),
                        IDLotto = datiRigheDdt[i, "IDLotto"].ToTrimmedString(),
                        Note = datiRigheDdt[i, "Note"].ToTrimmedString()
                    };
                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(righeDdt);
                }

                foreach (KeyValuePair<string, string> informazioniTipoDocumento in new Dictionary<string, string>
                         {
                             { ID_TIPO_DOCUMENTO_FATTURA_IMMEDIATA, "Fattura immediata" },
                             { ID_TIPO_DOCUMENTO_FATTURA_DIFFERITA, "Fattura differita" },
                             { ID_TIPO_DOCUMENTO_FATTURA_ACCOMPAGNATORIA, "Fattura accompagnatoria" },
                             { ID_TIPO_DOCUMENTO_NOTA_CREDITO, "Nota credito" },
                             { ID_TIPO_DOCUMENTO_DDT, "Ddt" },
                             { ID_TIPO_DOCUMENTO_DDT_FORNITORI, "Ddt fornitori" },
                             { ID_TIPO_DOCUMENTO_ORDINE_STORICO, "Ordine" }
                         })

                    this.ConnessioneiSellOUT.EseguiInserimentoRigaDatiiSell(new RigaDatiTipiDocumenti
                    {
                        IDTipoDocumento =
                            //"SMART_" +
                            informazioniTipoDocumento.Key,
                        DescrizioneTipoDocumento = informazioniTipoDocumento.Value,
                        GestioneAnagraficaIntestatario = GestioneAnagraficaIntestatario.Gestito,
                        GestioneAnagraficaDestinatario = GestioneAnagraficaDestinatario.Gestito,
                        GestioneAnagraficaRiferimento = GestioneAnagraficaRiferimento.Gestito,
                        GestioneNote = GestioneNote.Gestito,
                        GestionePagamento = GestionePagamento.Gestito,
                        GestioneListino = GestioneListino.Gestito,
                        GestioneLotti = Utilita.ValoriTabelle.TipiDocumenti.GestioneLotti.Gestito,
                        AttivitaPassivita = (Utilita.ValoriTabelle.TipiDocumenti.AttivitaPassivita)AttivitaPassivita.Attivo,
                        GestioneDeposito = GestioneDeposito.Gestito,
                        GestioneScontoChiusura = GestioneScontoChiusura.Gestito,
                        GestioneVariantiArticoli = 1,
                        GestioneDataConsegna = GestioneDataConsegna.Gestito,
                        GestioneTipoSpedizione = GestioneTipoSpedizione.Gestito,
                        GestioneVettore = GestioneVettore.Gestito
                    });
            }

            return new RisultatoConDescrizione(true);
        }

        internal Dictionary<string, decimal[]> PrelevaListaSconti()
        {
            Dictionary<string, decimal[]> dizionarioTabellaTsm = new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);

            using (TabellaDati datiSconti = this.ConnessioneSmart.CaricaTabella("tsm"))
            {
                var listaPercentualiSconti = new List<decimal>();
                for (int i = 0; i < datiSconti.NumeroRighe; i++)
                {
                    listaPercentualiSconti.Clear();
                    bool isMaggiorazione = datiSconti[i, "SCONTO_MAGGIORAZIONE"].ToTrimmedString() == "maggiorazione";
                    decimal moltiplicatore = isMaggiorazione ? -1 : 1;

                    for (int j = 1; j <= 10; j++)
                    {
                        decimal sconto = j <= 9 ? datiSconti[i, $"PERCENTUALE_0{j}"].ToDecimal() : datiSconti[i, $"PERCENTUALE_{j}"].ToDecimal();
                        if (sconto != 0)
                            listaPercentualiSconti.Add(sconto * moltiplicatore);
                    }

                    dizionarioTabellaTsm[datiSconti[i, "CODICE"].ToTrimmedString()] = listaPercentualiSconti.ToArray();
                }
            }

            return dizionarioTabellaTsm;
        }

        // Import - API

        private RisultatoConDescrizione Autentica(DefaultApi apiInstance)
        {
            // 1 - Generazione del token tramite api key e psw
            // 2 - Generazione refresh token tramite token creato con api key e psw
            // 3 - Generazione token questa volta utilizzando il refresh token
            // 4 - Ogni volta che il token scade utilizzare il refresh token per rigenerarlo

            var tokenDatiAusiliari = this.RilevaTokenDatiAusiliari();
            if (!this.RilevaValiditaToken(tokenDatiAusiliari))
            {
                var refreshTokenDatiAusiliari = this.RilevaRefreshTokenDatiAusiliari();
                if (!this.RilevaValiditaRefreshToken(refreshTokenDatiAusiliari))
                {
                    // 1 - Generazione del token tramite api key e psw
                    var apiKey = this.RilevaValoreParametro("apiKey").ToTrimmedString();
                    var apiPassword = this.RilevaValoreParametro("password").ToTrimmedString();
                    var responseBasicAuthToken = apiInstance.ApiAuthTokenPostWithHttpInfo(apiKey, apiPassword, ApiClient.NULL_VALUE);
                    if (responseBasicAuthToken.StatusCode == (int)HttpStatusCode.OK
                        && responseBasicAuthToken.Data != null
                        && !string.IsNullOrWhiteSpace(responseBasicAuthToken.Data.Token))
                    {
                        Configuration.Default.ApiKey.Add("X-AUTH-TOKEN", responseBasicAuthToken.Data.Token);
                        // 2 - Generazione refresh token tramite token creato con api key e psw
                        var responseRefreshToken = apiInstance.ApiAuthRefreshGeneratePostWithHttpInfo();
                        if (responseRefreshToken.StatusCode == (int)HttpStatusCode.OK
                            && responseRefreshToken.Data != null
                            && !string.IsNullOrWhiteSpace(responseRefreshToken.Data.Token))
                        {
                            this.RegistraRefreshTokenDatiAusiliari(responseRefreshToken.Data);
                        }
                    }
                    else
                    {
                        return new RisultatoConDescrizione(false, responseBasicAuthToken.ToString());
                    }
                }

                refreshTokenDatiAusiliari = this.RilevaRefreshTokenDatiAusiliari();
                if (!this.RilevaValiditaRefreshToken(refreshTokenDatiAusiliari))
                {
                    return new RisultatoConDescrizione(false, "Errore durante il recupero di un refresh token valido");
                }

                // 3 - Generazione token questa volta utilizzando il refresh token
                try
                {
                    var responseAuthWithRefreshToken = apiInstance.ApiAuthTokenPostWithHttpInfo(ApiClient.NULL_VALUE, ApiClient.NULL_VALUE, refreshTokenDatiAusiliari.Token);
                    if (responseAuthWithRefreshToken.StatusCode == (int)HttpStatusCode.OK
                        && responseAuthWithRefreshToken.Data != null
                        && !string.IsNullOrWhiteSpace(responseAuthWithRefreshToken.Data.Token))
                    {
                        this.RegistraTokenDatiAusiliari(responseAuthWithRefreshToken.Data);
                    }
                    else
                    {
                        this.RimuoviRefreshTokenDatiAusiliari();
                        this.Autentica(apiInstance);
                    }
                }
                catch
                {
                    this.RimuoviRefreshTokenDatiAusiliari();
                    this.Autentica(apiInstance);
                }

                tokenDatiAusiliari = this.RilevaTokenDatiAusiliari();
            }

            if (this.RilevaValiditaToken(tokenDatiAusiliari))
            {
                Configuration.Default.ApiKey["X-AUTH-TOKEN"] = tokenDatiAusiliari.Token;
            }

            if (!Configuration.Default.ApiKey.ContainsKey("X-AUTH-TOKEN")
                || string.IsNullOrWhiteSpace(Configuration.Default.ApiKey["X-AUTH-TOKEN"]))
                return new RisultatoConDescrizione(false, "Impossibile rilevare un token di autenticazione valido");

            return new RisultatoConDescrizione(true);
        }

        protected override RisultatoElaborazione ElaborazioneDocumentiIN(StrutturaDatiDocumento[] documenti, IInformazioniStrutture informazioniStrutture)
        {
            //http://192.168.1.230:8080/crm/bundles/goapi/ordine/index.html#/
            string uriServer = this.RilevaValoreParametro("URLApi").ToTrimmedString();
            if (string.IsNullOrWhiteSpace(uriServer))
                return new RisultatoElaborazione(RisultatoElaborazione.TipiEsitoElaborazione.DaElaborare, "URL Server Api non configurata");

            Configuration.Default.BasePath = uriServer;
            var apiInstance = new DefaultApi();

            var risultatoAutenticazione = this.Autentica(apiInstance);
            if (!risultatoAutenticazione.Esito)
                return new RisultatoElaborazione(risultatoAutenticazione.Esito, risultatoAutenticazione.Descrizione);

            // try
            // {
            //     var result = apiInstance.ApiOrdineInserisciPostWithHttpInfo(
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         ApiClient.NULL_VALUE,
            //         new List<ApiordineinserisciRighe>());
            //     Debug.WriteLine(result);
            // }
            // catch (Exception e)
            // {
            //     this.RegistraLog("Errore inserimento documento", TipiLog.Errore, e.ToString());
            // }

            // return new RisultatoElaborazione(RisultatoElaborazione.TipiEsitoElaborazione.NonElaborato, "");

            var dizionarioTabellaScontiStringaConCodici = new Dictionary<string, InsiemeInsensitive>(StringComparer.OrdinalIgnoreCase);
            foreach (var rigaScontoTsm in this.PrelevaListaSconti())
            {
                string stringaSconti = string.Join("+", rigaScontoTsm.Value);
                if (!dizionarioTabellaScontiStringaConCodici.ContainsKey(stringaSconti))
                    dizionarioTabellaScontiStringaConCodici[stringaSconti] = new InsiemeInsensitive();

                dizionarioTabellaScontiStringaConCodici[stringaSconti].Add(rigaScontoTsm.Key);
            }

            foreach (var documento in documenti)
            {
                string codiceScontoAnagrafica;
                var qScontoPerAnagrafica = new CompilatoreQueryDiSelezione("cli");
                qScontoPerAnagrafica.Filtro.AggiungiElementoFiltroStandard("CODICE", documento.IDAnagraficaIntestatario);
                using (TabellaDati datiAnagraficaSconto = this.ConnessioneSmart.EseguiSelezione(qScontoPerAnagrafica))
                {
                    codiceScontoAnagrafica = datiAnagraficaSconto.NumeroRighe > 0 ? datiAnagraficaSconto[0, "TSM_CODICE"].ToTrimmedString() : string.Empty;
                }

                List<ApiordineinserisciRighe> listaRighe = new List<ApiordineinserisciRighe>();
                foreach (var riga in documento.RigheDocumento)
                {
                    string codiceSconto = string.Empty;
                    // Gestione degli sconti nella fase di import
                    if (!dizionarioTabellaScontiStringaConCodici.TryGetValue(
                            this.GeneraStringaScontiRigaDocumento(riga),
                            out InsiemeInsensitive insiemeCorrispondenzeStringaSconti))
                    {
#warning Cambiare quando verrà aggiunta la generazione di nuovi codici nella tsm
#warning Attualmente da Castellini esiste il codice 0, pertanto viene sempre trovato un codice anche quando non ci sono sconti. Capire in futuro se è una logica comune che lo 0 esiste sempreo se va gestita diversamente
                        return new RisultatoElaborazione(false, "Impossibile rilevare il codice sconto per la riga documento " + riga.IDRigaDocumento);
                    }

                    if (insiemeCorrispondenzeStringaSconti.Contains(codiceScontoAnagrafica))
                    {
                        codiceSconto = codiceScontoAnagrafica;
                    }
                    else
                    {
                        var qScontoPerRigheListino = new CompilatoreQueryDiSelezione("lsv");
                        qScontoPerRigheListino.Filtro.AggiungiElementoFiltroStandard("ART_CODICE", riga.IDArticolo);
                        qScontoPerRigheListino.Filtro.AggiungiElementoFiltroStandard("TLV_CODICE", riga.IDListino);
                        using (TabellaDati datiRigheListino = this.ConnessioneSmart.EseguiSelezione(qScontoPerRigheListino))
                        {
                            string codiceScontoListino = datiRigheListino.NumeroRighe > 0 ? datiRigheListino[0, "TSM_CODICE"].ToTrimmedString() : string.Empty;
                            if (insiemeCorrispondenzeStringaSconti.Contains(codiceScontoListino))
                            {
                                codiceSconto = codiceScontoListino;
                            }
                            else
                            {
                                codiceSconto = insiemeCorrispondenzeStringaSconti.Count > 0 ? insiemeCorrispondenzeStringaSconti.First() : "";
                            }
                        }
                    }

                    listaRighe.Add(
                        new ApiordineinserisciRighe(
                            riga.DescrizioneRiga,
                            riga.IDArticolo,
                            (int)riga.Quantita,
                            codiceSconto,
                            "",
                            riga.IDAliquotaIVA,
                            (float)riga.Prezzo,
                            riga.Note,
                            null
                        )
                    );
                }

                string idAnagraficaDestinatario = string.Empty;
                if (!documento.IDAnagraficaIntestatario.UgualeCaseInsensitive(documento.IDAnagraficaDestinatario)
                    && documento.IDAnagraficaDestinatario.Contains("_"))
                {
                    idAnagraficaDestinatario = documento.IDAnagraficaDestinatario.Split('_')[1];
                }

#warning Manca da gestire gli sconti chiusura, che attualmente non gestiamo poichè manca la generazione di nuovi codici
                var risposta = apiInstance.ApiOrdineInserisciPostWithHttpInfo(
                    documento.IDAnagraficaIntestatario,
                    documento.IDDeposito,
                    documento.IDTipoDocumento,
                    idAnagraficaDestinatario,
                    documento.NumeroDocumento.ToString(),
                    PL.Utilita.FunzioniDati.ConvertiStringaDataYYYYMMDDHHMMSSMMInData(documento.DataDocumento).ToString("dd-mm-yyyy"),
                    PL.Utilita.FunzioniDati.ConvertiStringaDataYYYYMMDDHHMMSSMMInData(documento.DataConsegna).ToString("dd-mm-yyyy"),
                    "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    documento.IDValuta,
                    documento.IDPagamento,
                    documento.IDListino,
                    "",
                    documento.IDOperatoreOrigineDati,
                    "",
                    documento.Note,
                    listaRighe);

                // risposta.StatusCode

                if (risposta.StatusCode == 400 || risposta.StatusCode == 500)
                {
                    return new RisultatoElaborazione(false, $"Errore creazione testata documento {risposta.StatusCode.ToString()}");
                }

                documento.IDEsternoElaborazioneElemento = risposta.Data.IdOvt.ToString();
            }

            return new RisultatoElaborazione(true);
        }

        private string GeneraStringaScontiRigaDocumento(StrutturaDatiRigaDocumento riga)
        {
            List<decimal> listaSconti = new List<decimal>();

            void AggiungiScontiSeCompilati(decimal[] scontiDaAggiungere)
            {
                foreach (var singoloSconto in scontiDaAggiungere)
                    if (singoloSconto != 0)
                        listaSconti.Add(singoloSconto);
            }

            AggiungiScontiSeCompilati(new decimal[]
            {
                riga.Sconto1,
                riga.Sconto2,
                riga.Sconto3,
                riga.Sconto4,
                riga.Sconto5,
                riga.Sconto6,
                riga.Sconto7,
                riga.Sconto8
            });
            return string.Join("+", listaSconti);
        }

        //     var URLApi = this.RilevaValoreParametro("URLApi").ToTrimmedString();
        //     if (string.IsNullOrWhiteSpace(URLApi))
        //     {
        //         return new RisultatoElaborazione(false, "Valore URL Api non impostato");
        //     }
        //
        //     using (HttpClient httpClient = new HttpClient())
        //     {
        //         foreach (var documento in documenti)
        //         {
        //             try
        //             {
        //                 var parametri = new Dictionary<string, string>();
        //
        //                 parametri.Add("cliCodice", documento.IDAnagraficaIntestatario);
        //                 parametri.Add("tmaCodice", documento.IDDeposito);
        //                 parametri.Add("tdoCodice", documento.IDTipoDocumento);
        //                 parametri.Add("indCodice", "");
        //                 parametri.Add("numeroDocumento", documento.NumeroDocumento.ToString());
        //
        //                 string dataDocumento = PL.Utilita.FunzioniDati.ConvertiStringaDataYYYYMMDDHHMMSSMMInData(documento.DataDocumento).ToString("dd-mm-yyyy");
        //                 parametri.Add("dataDocumento", dataDocumento);
        //
        //                 string dataConsegna = PL.Utilita.FunzioniDati.ConvertiStringaDataYYYYMMDDHHMMSSMMInData(documento.DataConsegna).ToString("dd-mm-yyyy");
        //                 parametri.Add("dataConsegna", dataConsegna);
        //
        //                 parametri.Add("descrizione1", "");
        //                 parametri.Add("descrizione2", "");
        //                 parametri.Add("via", "");
        //                 parametri.Add("cap", "");
        //                 parametri.Add("citta", "");
        //                 parametri.Add("provincia", "");
        //                 parametri.Add("tvaCodice", documento.IDValuta);
        //                 parametri.Add("tpaCodice", documento.IDPagamento);
        //                 parametri.Add("tlvCodice", documento.IDListino);
        //                 parametri.Add("tsmCodice", "");
        //                 parametri.Add("tsmCodiceArt", "");
        //                 parametri.Add("tagCodice", documento.IDOperatoreOrigineDati);
        //                 parametri.Add("codiceContratto", "");
        //                 parametri.Add("note", documento.Note);
        //
        //                 var numeroRiga = 0;
        //                 foreach (var riga in documento.RigheDocumento)
        //                 {
        //                     parametri.Add($"righe[{numeroRiga}].descrizione", riga.DescrizioneRiga);
        //                     parametri.Add($"righe[{numeroRiga}].artCodice", riga.IDArticolo);
        //                     parametri.Add($"righe[{numeroRiga}].quantita", riga.Quantita.ToString());
        //                     parametri.Add($"righe[{numeroRiga}].tsmCodice", "");
        //                     parametri.Add($"righe[{numeroRiga}].tsmCodiceArt", "");
        //                     parametri.Add($"righe[{numeroRiga}].tivCodice", riga.IDAliquotaIVA);
        //                     parametri.Add($"righe[{numeroRiga}].prezzo", "");
        //                     parametri.Add($"righe[{numeroRiga}].note", riga.Note);
        //                     parametri.Add($"[righe{numeroRiga}].tipoMovimento", "");
        //                     
        //                     numeroRiga++;
        //                 }
        //
        //                 var risultato = httpClient.PostAsync(URLApi + "/api/ordine/inserisci", new FormUrlEncodedContent(parametri)).GetAwaiter().GetResult();
        //                 risultato.EnsureSuccessStatusCode();
        //                 if ((int)risultato.StatusCode == 400 || (int)risultato.StatusCode == 500)
        //                 {
        //                     return new RisultatoElaborazione(false, $"Errore creazione testata documento {risultato.StatusCode.ToString()}");
        //                 }
        //
        //                 var risposta = risultato.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        //                 var risultatoOperazione = JsonConvert.DeserializeObject<RisultatoRichiestaCrezioneOrdine>(risposta);
        //
        //                 documento.IDEsternoElaborazioneElemento = risultatoOperazione.idOvt;
        //                 documento.RiferimentoEsternoElaborazioneElemento = risultatoOperazione.idOvt;
        //                 
        //             }
        //             catch (Exception ex)
        //             {
        //                 return new RisultatoElaborazione(false, $"Errore creazione testata documento {ex}");
        //             }
        //         }
        //     }
        //
        //     return new RisultatoElaborazione(true);
        // }

        private InlineResponse200 RilevaTokenDatiAusiliari()
        {
            var tokenDatiAusiliari = this.DatiAusiliari.RilevaDatoAusiliario("SWAGGERAPI", "TOKEN").ToTrimmedString();
            try
            {
                return ApiClient.DeserializeObjectFromJson<InlineResponse200>(tokenDatiAusiliari);
            }
            catch
            {
                return null;
            }
        }

        private void RegistraTokenDatiAusiliari(InlineResponse200 datiToken)
        {
            this.DatiAusiliari.InserisciDatoAusiliario("SWAGGERAPI", "TOKEN", datiToken.ToJson());
        }

        private InlineResponse2001 RilevaRefreshTokenDatiAusiliari()
        {
            var tokenDatiAusiliari = this.DatiAusiliari.RilevaDatoAusiliario("SWAGGERAPI", "REFRESH_TOKEN").ToTrimmedString();
            try
            {
                return ApiClient.DeserializeObjectFromJson<InlineResponse2001>(tokenDatiAusiliari);
            }
            catch
            {
                return null;
            }
        }

        private void RegistraRefreshTokenDatiAusiliari(InlineResponse2001 datiToken)
        {
            this.DatiAusiliari.InserisciDatoAusiliario("SWAGGERAPI", "REFRESH_TOKEN", ApiClient.SerializeObjectToJson(datiToken));
        }

        private void RimuoviRefreshTokenDatiAusiliari()
        {
            this.DatiAusiliari.EliminaDatiAusiliari("SWAGGERAPI", "REFRESH_TOKEN");
        }

        private bool RilevaValiditaRefreshToken(InlineResponse2001 datiToken)
        {
            return datiToken != null
                   && (string.IsNullOrWhiteSpace(datiToken.Expires) || DateTime.Parse(datiToken.Expires) >= DateTime.Now.AddMinutes(5));
        }

        private bool RilevaValiditaToken(InlineResponse200 datiToken)
        {
            return datiToken != null
                   && (string.IsNullOrWhiteSpace(datiToken.Expires) || DateTime.Parse(datiToken.Expires) >= DateTime.Now.AddMinutes(5));
        }

        #endregion

        #region Attività base connettore

        protected override void PreparaAttivitaBaseOUT()
        {
            this.CreaAttivitaBase(
                "OUT",
                "Esporta dati a iSell",
                FasiElaborazioni.AnagraficheOUT,
                FasiElaborazioni.ArticoliOUT,
                FasiElaborazioni.DocumentiOUT,
                FasiElaborazioni.ListiniOUT
            );
        }

        protected override void PreparaAttivitaBaseIN()
        {
            this.CreaAttivitaBase(
                "IN",
                "Importa dati da iSell",
                FasiElaborazioni.DocumentiIN
            );
        }

        #endregion
    }
}