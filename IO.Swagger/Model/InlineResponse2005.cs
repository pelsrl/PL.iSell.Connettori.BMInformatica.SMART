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
    /// InlineResponse2005
    /// </summary>
    [DataContract]
        public partial class InlineResponse2005 :  IEquatable<InlineResponse2005>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InlineResponse2005" /> class.
        /// </summary>
        /// <param name="idOvt">L&#x27;ID della testata dell&#x27;ordine a cui sono state aggiunte le righe..</param>
        /// <param name="idRighe">Gli ID delle righe inserite..</param>
        public InlineResponse2005(int? idOvt = default(int?), List<int?> idRighe = default(List<int?>))
        {
            this.IdOvt = idOvt;
            this.IdRighe = idRighe;
        }
        
        /// <summary>
        /// L&#x27;ID della testata dell&#x27;ordine a cui sono state aggiunte le righe.
        /// </summary>
        /// <value>L&#x27;ID della testata dell&#x27;ordine a cui sono state aggiunte le righe.</value>
        [DataMember(Name="idOvt", EmitDefaultValue=false)]
        public int? IdOvt { get; set; }

        /// <summary>
        /// Gli ID delle righe inserite.
        /// </summary>
        /// <value>Gli ID delle righe inserite.</value>
        [DataMember(Name="idRighe", EmitDefaultValue=false)]
        public List<int?> IdRighe { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class InlineResponse2005 {\n");
            sb.Append("  IdOvt: ").Append(IdOvt).Append("\n");
            sb.Append("  IdRighe: ").Append(IdRighe).Append("\n");
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
            return this.Equals(input as InlineResponse2005);
        }

        /// <summary>
        /// Returns true if InlineResponse2005 instances are equal
        /// </summary>
        /// <param name="input">Instance of InlineResponse2005 to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(InlineResponse2005 input)
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
                    this.IdRighe == input.IdRighe ||
                    this.IdRighe != null &&
                    input.IdRighe != null &&
                    this.IdRighe.SequenceEqual(input.IdRighe)
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
                if (this.IdRighe != null)
                    hashCode = hashCode * 59 + this.IdRighe.GetHashCode();
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
