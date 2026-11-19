using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MreHunter.Api.Models
{
    public class AboutUsRequest
    {
        [Required]
        [MaxLength(5000)]
        public string? HeroTitle { get; set; }

        [Required]
        [MaxLength(5000)]
        public string? HeroText { get; set; }

        [MaxLength(500)]
        public string? HeroVideoUrl { get; set; }

        [Required]
        [MaxLength(200)]
        public string? WhoTitle { get; set; }

        [Required]
        [MaxLength(5000)]
        public string? WhoText { get; set; }

        [Required]
        [MaxLength(200)]
        public string? ChooseTitle { get; set; }

        [Required]
        [MaxLength(5000)]
        public string? ChooseText { get; set; }

        [Required]
        [MaxLength(200)]
        public string? MissionTitle { get; set; }

        [Required]
        [MaxLength(5000)]
        public string? MissionText { get; set; }

        [Required]
        [MaxLength(200)]
        public string? VisionTitle { get; set; }

        [Required]
        [MaxLength(5000)]
        public string? VisionText { get; set; }

        [Required]
        [MaxLength(200)]
        public string? BenefitsTitle { get; set; }

        [Required]
        [MaxLength(300)]
        public string? BenefitsSubtitle { get; set; }

        public List<BenefitItem>? Benefits { get; set; }

        /// <summary>
        /// Alternativa a <see cref="Benefits"/> para multipart/form-data: la lista de beneficios como texto JSON,
        /// por ejemplo [{"Icon":"alert-icon","Text":"Alertas Inteligentes"}]. Es lo que envía el CMS de Angular.
        /// </summary>
        public string? BenefitsItemsJson { get; set; }

        [Required]
        [MaxLength(200)]
        public string? CtaTitle { get; set; }

        [Required]
        [MaxLength(100)]
        public string? CtaButtonText { get; set; }

        [MaxLength(500)]
        public string? CtaButtonLink { get; set; }

        // Sin [MaxLength]: ese validador solo mide textos y listas; aplicado a un archivo
        // lanzaba una excepción (500) al subir la imagen. El tipo y tamaño se validan en SaveImage.
        public IFormFile? CtaImage { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }
    }

    public class BenefitItem
    {
        [Required]
        [MaxLength(100)]
        public string? Icon { get; set; }

        [Required]
        [MaxLength(500)]
        public string? Text { get; set; }
    }
}