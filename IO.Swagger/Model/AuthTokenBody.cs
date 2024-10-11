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
    /// AuthTokenBody
    /// </summary>
    [DataContract]
        public partial class AuthTokenBody :  IEquatable<AuthTokenBody>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthTokenBody" /> class.
        /// </summary>
        /// <param name="apiKey">La apiKey dell&#x27;utente (default to &quot;&quot;).</param>
        /// <param name="password">La password dell&#x27;utente (default to &quot;&quot;).</param>
        /// <param name="refresh">Il refresh token (default to &quot;&quot;).</param>
        public AuthTokenBody(string apiKey = "", string password = "", string refresh = "")
        {
            // use default value if no "apiKey" provided
            if (apiKey == null)
            {
                this.ApiKey = "";
            }
            else
            {
                this.ApiKey = apiKey;
            }
            // use default value if no "password" provided
            if (password == null)
            {
                this.Password = "";
            }
            else
            {
                this.Password = password;
            }
            // use default value if no "refresh" provided
            if (refresh == null)
            {
                this.Refresh = "";
            }
            else
            {
                this.Refresh = refresh;
            }
        }
        
        /// <summary>
        /// La apiKey dell&#x27;utente
        /// </summary>
        /// <value>La apiKey dell&#x27;utente</value>
        [DataMember(Name="apiKey", EmitDefaultValue=false)]
        public string ApiKey { get; set; }

        /// <summary>
        /// La password dell&#x27;utente
        /// </summary>
        /// <value>La password dell&#x27;utente</value>
        [DataMember(Name="password", EmitDefaultValue=false)]
        public string Password { get; set; }

        /// <summary>
        /// Il refresh token
        /// </summary>
        /// <value>Il refresh token</value>
        [DataMember(Name="refresh", EmitDefaultValue=false)]
        public string Refresh { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AuthTokenBody {\n");
            sb.Append("  ApiKey: ").Append(ApiKey).Append("\n");
            sb.Append("  Password: ").Append(Password).Append("\n");
            sb.Append("  Refresh: ").Append(Refresh).Append("\n");
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
            return this.Equals(input as AuthTokenBody);
        }

        /// <summary>
        /// Returns true if AuthTokenBody instances are equal
        /// </summary>
        /// <param name="input">Instance of AuthTokenBody to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AuthTokenBody input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.ApiKey == input.ApiKey ||
                    (this.ApiKey != null &&
                    this.ApiKey.Equals(input.ApiKey))
                ) && 
                (
                    this.Password == input.Password ||
                    (this.Password != null &&
                    this.Password.Equals(input.Password))
                ) && 
                (
                    this.Refresh == input.Refresh ||
                    (this.Refresh != null &&
                    this.Refresh.Equals(input.Refresh))
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
                if (this.ApiKey != null)
                    hashCode = hashCode * 59 + this.ApiKey.GetHashCode();
                if (this.Password != null)
                    hashCode = hashCode * 59 + this.Password.GetHashCode();
                if (this.Refresh != null)
                    hashCode = hashCode * 59 + this.Refresh.GetHashCode();
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
