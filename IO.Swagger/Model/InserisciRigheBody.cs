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
    /// InserisciRigheBody
    /// </summary>
    [DataContract]
        public partial class InserisciRigheBody :  IEquatable<InserisciRigheBody>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InserisciRigheBody" /> class.
        /// </summary>
        /// <param name="idOvt">ID della testata dell&#x27;ordine (required).</param>
        /// <param name="righe">Un array di oggetti, ognuno contenente i parametri necessari per le righe dell&#x27;ordine, come specificato nell&#x27;API &#x60;/api/ordine/riga/inserisci&#x60;. (required).</param>
        public InserisciRigheBody(int? idOvt = default(int?), List<ApiordinerigainseriscirigheRighe> righe = default(List<ApiordinerigainseriscirigheRighe>))
        {
            // to ensure "idOvt" is required (not null)
            if (idOvt == null)
            {
                throw new InvalidDataException("idOvt is a required property for InserisciRigheBody and cannot be null");
            }
            else
            {
                this.IdOvt = idOvt;
            }
            // to ensure "righe" is required (not null)
            if (righe == null)
            {
                throw new InvalidDataException("righe is a required property for InserisciRigheBody and cannot be null");
            }
            else
            {
                this.Righe = righe;
            }
        }
        
        /// <summary>
        /// ID della testata dell&#x27;ordine
        /// </summary>
        /// <value>ID della testata dell&#x27;ordine</value>
        [DataMember(Name="idOvt", EmitDefaultValue=false)]
        public int? IdOvt { get; set; }

        /// <summary>
        /// Un array di oggetti, ognuno contenente i parametri necessari per le righe dell&#x27;ordine, come specificato nell&#x27;API &#x60;/api/ordine/riga/inserisci&#x60;.
        /// </summary>
        /// <value>Un array di oggetti, ognuno contenente i parametri necessari per le righe dell&#x27;ordine, come specificato nell&#x27;API &#x60;/api/ordine/riga/inserisci&#x60;.</value>
        [DataMember(Name="righe", EmitDefaultValue=false)]
        public List<ApiordinerigainseriscirigheRighe> Righe { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class InserisciRigheBody {\n");
            sb.Append("  IdOvt: ").Append(IdOvt).Append("\n");
            sb.Append("  Righe: ").Append(Righe).Append("\n");
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
            return this.Equals(input as InserisciRigheBody);
        }

        /// <summary>
        /// Returns true if InserisciRigheBody instances are equal
        /// </summary>
        /// <param name="input">Instance of InserisciRigheBody to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(InserisciRigheBody input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.IdOvt == input.IdOvt ||
                    (this.IdOvt != null &&
                    this.IdOvt.Equals(input.IdOvt))
                ) && 
                (
                    this.Righe == input.Righe ||
                    this.Righe != null &&
                    input.Righe != null &&
                    this.Righe.SequenceEqual(input.Righe)
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
                if (this.IdOvt != null)
                    hashCode = hashCode * 59 + this.IdOvt.GetHashCode();
                if (this.Righe != null)
                    hashCode = hashCode * 59 + this.Righe.GetHashCode();
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
