using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using System.Globalization;

namespace IO.Swagger.Client
{
    [DataContract]
    public class RigaInserimentoOrdine : ApiordineinserisciRighe
    {
        [DataMember(Name = "PosizioneInArray", EmitDefaultValue = false)]
        [JsonProperty]
        public int PosizioneInArray { get; set; }

        public RigaInserimentoOrdine(
            int posizioneInArray,
            string descrizione = "",
            string artCodice = "",
            int? quantita = default(int?),
            string tsmCodice = "",
            string tsmCodiceArt = "",
            string tivCodice = "",
            float? prezzo = default(float?),
            string note = "",
            TipoMovimentoEnum? tipoMovimento = TipoMovimentoEnum.Normale)
            : base(descrizione, artCodice, quantita, tsmCodice, tsmCodiceArt, tivCodice, prezzo, note, tipoMovimento)
        {
            this.PosizioneInArray = posizioneInArray;
        }


        public override string ToString()
        {
            var daTornare = JsonConvert.SerializeObject(this, Formatting.Indented);
            return daTornare;
        }

        protected static readonly CultureInfo _cultureInfoPerGestionePrezzi = CultureInfo.GetCultureInfo("en");
        public List<GetOrPostParameter> ToMultiDataParameters()
        {
            var daTornare = new List<GetOrPostParameter>();

            void VerificaEAggiungiCampo(string nomeCampo, string valore)
            {
                if (valore != null)
                {
                    daTornare.Add(new GetOrPostParameter($"righe[{this.PosizioneInArray}][{nomeCampo}]", valore));
                }
            }

            VerificaEAggiungiCampo("descrizione", this.Descrizione);
            VerificaEAggiungiCampo("artCodice", this.ArtCodice);
            VerificaEAggiungiCampo("quantita", this.Quantita.ToString());
            VerificaEAggiungiCampo("tsmCodice", this.TsmCodice);
            VerificaEAggiungiCampo("tsmCodiceArt", this.TsmCodiceArt);
            VerificaEAggiungiCampo("tivCodice", this.TivCodice);
            VerificaEAggiungiCampo("prezzo", ((decimal)this.Prezzo).ToString(_cultureInfoPerGestionePrezzi));
            VerificaEAggiungiCampo("note", this.Note);
            VerificaEAggiungiCampo("tipoMovimento", (Enum.GetName(typeof(TipoMovimentoEnum), this.TipoMovimento)).ToLower().ToString());

            return daTornare;
        }
    }
}