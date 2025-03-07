/* 
 * API Inserimento Ordine
 *
 * Documentazione delle API per l'inserimento di un ordine.   **Tutti i parametri che non hanno un `required` affianco sono opzionali.** In più, durante la prova togliere la spunta da \"Send empty value\".  Così, il parametro non viene passato; se non viene tolto, allora il parametro viene passato come stringa vuota.   
 *
 * OpenAPI spec version: 1.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = IO.Swagger.Client.SwaggerDateConverter;
namespace IO.Swagger.Model
{
    /// <summary>
    /// ApiordinerigainseriscirigheRighe
    /// </summary>
    [DataContract]
        public partial class ApiordinerigainseriscirigheRighe :  IEquatable<ApiordinerigainseriscirigheRighe>, IValidatableObject
    {
        /// <summary>
        /// Tipo di movimento
        /// </summary>
        /// <value>Tipo di movimento</value>
        [JsonConverter(typeof(StringEnumConverter))]
                public enum TipoMovimentoEnum
        {
            /// <summary>
            /// Enum Normale for value: normale
            /// </summary>
            [EnumMember(Value = "normale")]
            Normale = 1,
            /// <summary>
            /// Enum Omaggio for value: omaggio
            /// </summary>
            [EnumMember(Value = "omaggio")]
            Omaggio = 2,
            /// <summary>
            /// Enum Scontomerce for value: sconto merce
            /// </summary>
            [EnumMember(Value = "sconto merce")]
            Scontomerce = 3,
            /// <summary>
            /// Enum Omaggionorivalsa for value: omaggio no rivalsa
            /// </summary>
            [EnumMember(Value = "omaggio no rivalsa")]
            Omaggionorivalsa = 4        }
        /// <summary>
        /// Tipo di movimento
        /// </summary>
        /// <value>Tipo di movimento</value>
        [DataMember(Name="tipoMovimento", EmitDefaultValue=false)]
        public TipoMovimentoEnum? TipoMovimento { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiordinerigainseriscirigheRighe" /> class.
        /// </summary>
        /// <param name="descrizione">Descrizione della riga dell&#x27;ordine (required) (default to &quot;&quot;).</param>
        /// <param name="artCodice">Codice dell&#x27;articolo (default to &quot;&quot;).</param>
        /// <param name="quantita">Quantità (required).</param>
        /// <param name="tsmCodice">Codice sconto (default to &quot;&quot;).</param>
        /// <param name="tsmCodiceArt">Codice sconto articolo (default to &quot;&quot;).</param>
        /// <param name="tivCodice">Codice dell&#x27;IVA (default to &quot;&quot;).</param>
        /// <param name="prezzo">Prezzo.</param>
        /// <param name="note">Note (default to &quot;&quot;).</param>
        /// <param name="tipoMovimento">Tipo di movimento (default to TipoMovimentoEnum.Normale).</param>
        public ApiordinerigainseriscirigheRighe(string descrizione = "", string artCodice = "", int? quantita = default(int?), string tsmCodice = "", string tsmCodiceArt = "", string tivCodice = "", float? prezzo = default(float?), string note = "", TipoMovimentoEnum? tipoMovimento = TipoMovimentoEnum.Normale)
        {
            // to ensure "descrizione" is required (not null)
            if (descrizione == null)
            {
                throw new InvalidDataException("descrizione is a required property for ApiordinerigainseriscirigheRighe and cannot be null");
            }
            else
            {
                this.Descrizione = descrizione;
            }
            // to ensure "quantita" is required (not null)
            if (quantita == null)
            {
                throw new InvalidDataException("quantita is a required property for ApiordinerigainseriscirigheRighe and cannot be null");
            }
            else
            {
                this.Quantita = quantita;
            }
            // use default value if no "artCodice" provided
            if (artCodice == null)
            {
                this.ArtCodice = "";
            }
            else
            {
                this.ArtCodice = artCodice;
            }
            // use default value if no "tsmCodice" provided
            if (tsmCodice == null)
            {
                this.TsmCodice = "";
            }
            else
            {
                this.TsmCodice = tsmCodice;
            }
            // use default value if no "tsmCodiceArt" provided
            if (tsmCodiceArt == null)
            {
                this.TsmCodiceArt = "";
            }
            else
            {
                this.TsmCodiceArt = tsmCodiceArt;
            }
            // use default value if no "tivCodice" provided
            if (tivCodice == null)
            {
                this.TivCodice = "";
            }
            else
            {
                this.TivCodice = tivCodice;
            }
            this.Prezzo = prezzo;
            // use default value if no "note" provided
            if (note == null)
            {
                this.Note = "";
            }
            else
            {
                this.Note = note;
            }
            // use default value if no "tipoMovimento" provided
            if (tipoMovimento == null)
            {
                this.TipoMovimento = TipoMovimentoEnum.Normale;
            }
            else
            {
                this.TipoMovimento = tipoMovimento;
            }
        }
        
        /// <summary>
        /// Descrizione della riga dell&#x27;ordine
        /// </summary>
        /// <value>Descrizione della riga dell&#x27;ordine</value>
        [DataMember(Name="descrizione", EmitDefaultValue=false)]
        public string Descrizione { get; set; }

        /// <summary>
        /// Codice dell&#x27;articolo
        /// </summary>
        /// <value>Codice dell&#x27;articolo</value>
        [DataMember(Name="artCodice", EmitDefaultValue=false)]
        public string ArtCodice { get; set; }

        /// <summary>
        /// Quantità
        /// </summary>
        /// <value>Quantità</value>
        [DataMember(Name="quantita", EmitDefaultValue=false)]
        public int? Quantita { get; set; }

        /// <summary>
        /// Codice sconto
        /// </summary>
        /// <value>Codice sconto</value>
        [DataMember(Name="tsmCodice", EmitDefaultValue=false)]
        public string TsmCodice { get; set; }

        /// <summary>
        /// Codice sconto articolo
        /// </summary>
        /// <value>Codice sconto articolo</value>
        [DataMember(Name="tsmCodiceArt", EmitDefaultValue=false)]
        public string TsmCodiceArt { get; set; }

        /// <summary>
        /// Codice dell&#x27;IVA
        /// </summary>
        /// <value>Codice dell&#x27;IVA</value>
        [DataMember(Name="tivCodice", EmitDefaultValue=false)]
        public string TivCodice { get; set; }

        /// <summary>
        /// Prezzo
        /// </summary>
        /// <value>Prezzo</value>
        [DataMember(Name="prezzo", EmitDefaultValue=false)]
        public float? Prezzo { get; set; }

        /// <summary>
        /// Note
        /// </summary>
        /// <value>Note</value>
        [DataMember(Name="note", EmitDefaultValue=false)]
        public string Note { get; set; }


        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ApiordinerigainseriscirigheRighe {\n");
            sb.Append("  Descrizione: ").Append(Descrizione).Append("\n");
            sb.Append("  ArtCodice: ").Append(ArtCodice).Append("\n");
            sb.Append("  Quantita: ").Append(Quantita).Append("\n");
            sb.Append("  TsmCodice: ").Append(TsmCodice).Append("\n");
            sb.Append("  TsmCodiceArt: ").Append(TsmCodiceArt).Append("\n");
            sb.Append("  TivCodice: ").Append(TivCodice).Append("\n");
            sb.Append("  Prezzo: ").Append(Prezzo).Append("\n");
            sb.Append("  Note: ").Append(Note).Append("\n");
            sb.Append("  TipoMovimento: ").Append(TipoMovimento).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as ApiordinerigainseriscirigheRighe);
        }

        /// <summary>
        /// Returns true if ApiordinerigainseriscirigheRighe instances are equal
        /// </summary>
        /// <param name="input">Instance of ApiordinerigainseriscirigheRighe to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(ApiordinerigainseriscirigheRighe input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Descrizione == input.Descrizione ||
                    (this.Descrizione != null &&
                    this.Descrizione.Equals(input.Descrizione))
                ) && 
                (
                    this.ArtCodice == input.ArtCodice ||
                    (this.ArtCodice != null &&
                    this.ArtCodice.Equals(input.ArtCodice))
                ) && 
                (
                    this.Quantita == input.Quantita ||
                    (this.Quantita != null &&
                    this.Quantita.Equals(input.Quantita))
                ) && 
                (
                    this.TsmCodice == input.TsmCodice ||
                    (this.TsmCodice != null &&
                    this.TsmCodice.Equals(input.TsmCodice))
                ) && 
                (
                    this.TsmCodiceArt == input.TsmCodiceArt ||
                    (this.TsmCodiceArt != null &&
                    this.TsmCodiceArt.Equals(input.TsmCodiceArt))
                ) && 
                (
                    this.TivCodice == input.TivCodice ||
                    (this.TivCodice != null &&
                    this.TivCodice.Equals(input.TivCodice))
                ) && 
                (
                    this.Prezzo == input.Prezzo ||
                    (this.Prezzo != null &&
                    this.Prezzo.Equals(input.Prezzo))
                ) && 
                (
                    this.Note == input.Note ||
                    (this.Note != null &&
                    this.Note.Equals(input.Note))
                ) && 
                (
                    this.TipoMovimento == input.TipoMovimento ||
                    (this.TipoMovimento != null &&
                    this.TipoMovimento.Equals(input.TipoMovimento))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Descrizione != null)
                    hashCode = hashCode * 59 + this.Descrizione.GetHashCode();
                if (this.ArtCodice != null)
                    hashCode = hashCode * 59 + this.ArtCodice.GetHashCode();
                if (this.Quantita != null)
                    hashCode = hashCode * 59 + this.Quantita.GetHashCode();
                if (this.TsmCodice != null)
                    hashCode = hashCode * 59 + this.TsmCodice.GetHashCode();
                if (this.TsmCodiceArt != null)
                    hashCode = hashCode * 59 + this.TsmCodiceArt.GetHashCode();
                if (this.TivCodice != null)
                    hashCode = hashCode * 59 + this.TivCodice.GetHashCode();
                if (this.Prezzo != null)
                    hashCode = hashCode * 59 + this.Prezzo.GetHashCode();
                if (this.Note != null)
                    hashCode = hashCode * 59 + this.Note.GetHashCode();
                if (this.TipoMovimento != null)
                    hashCode = hashCode * 59 + this.TipoMovimento.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}
