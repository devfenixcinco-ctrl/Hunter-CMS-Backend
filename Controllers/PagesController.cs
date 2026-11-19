using Microsoft.AspNetCore.Mvc;

namespace MreHunter.Api.Controllers
{
    /// <summary>
    /// Todo lo que necesita una página del sitio en UNA sola llamada (como /api/site/layout).
    /// Cada bloque es exactamente la respuesta del endpoint individual, porque se obtiene
    /// ejecutando esa misma acción: si un endpoint cambia, el bloque cambia igual.
    /// Si un bloque no tiene datos (el endpoint individual respondería 404), llega en null.
    /// Los endpoints individuales siguen funcionando.
    /// </summary>
    [ApiController]
    [Route("api/pages")]
    [Tags("Páginas (datos agrupados)")]
    public class PagesController : ControllerBase
    {
        /// <summary>Inicio: reemplaza /api/home, /api/noticias y /api/blog.</summary>
        [HttpGet("home")]
        public async Task<IActionResult> Home()
        {
            var home = From<HomeController>(c => c.Get());
            var noticias = From<NoticiasController>(c => c.GetAll());
            var blog = From<BlogController>(c => c.GetAll());
            await Task.WhenAll(home, noticias, blog);
            return Ok(new { home = home.Result, noticias = noticias.Result, blog = blog.Result });
        }

        /// <summary>GPS Convencional: reemplaza 5 llamadas.</summary>
        [HttpGet("conventional")]
        public async Task<IActionResult> Conventional()
        {
            var principalBanner = From<BenefitsHeroController>(c => c.Get());                // /api/conventional-principal-banner
            var benefits = From<ProductBenefitItemController>(c => c.GetAll());             // /api/conventional-benefits
            var highlight = From<BenefitHighlightController>(c => c.Get());                 // /api/conventional-highlight
            var infoSection = From<BenefitContentBlockController>(c => c.GetAll());         // /api/conventional-info-section
            var paymentMethods = From<PaymentMethodController>(c => c.GetAll());            // /api/payment-methods
            await Task.WhenAll(principalBanner, benefits, highlight, infoSection, paymentMethods);
            return Ok(new
            {
                principalBanner = principalBanner.Result,
                benefits = benefits.Result,
                highlight = highlight.Result,
                infoSection = infoSection.Result,
                paymentMethods = paymentMethods.Result
            });
        }

        /// <summary>GPS Portátil: reemplaza 4 llamadas.</summary>
        [HttpGet("portable")]
        public async Task<IActionResult> Portable()
        {
            var product = From<ProductsPortableController>(c => c.Get());                   // /api/portable-products
            var benefits = From<PortableBenefitsController>(c => c.GetAll());               // /api/portable-benefits
            var mediaSections = From<PortableMediaSectionsController>(c => c.GetAll());     // /api/portable-media-sections
            var paymentMethods = From<PaymentMethodController>(c => c.GetAll());            // /api/payment-methods
            await Task.WhenAll(product, benefits, mediaSections, paymentMethods);
            return Ok(new { product = product.Result, benefits = benefits.Result, mediaSections = mediaSections.Result, paymentMethods = paymentMethods.Result });
        }

        /// <summary>Dashcam: reemplaza 4 llamadas.</summary>
        [HttpGet("dashcam")]
        public async Task<IActionResult> Dashcam()
        {
            var product = From<ProductsDashcamController>(c => c.Get());                    // /api/products-dashcam
            var benefits = From<DashcamBenefitsController>(c => c.GetAll());                // /api/dashcam-benefits
            var mediaSections = From<DashcamMediaSectionsController>(c => c.GetAll());      // /api/dashcam-media-sections
            var paymentMethods = From<PaymentMethodController>(c => c.GetAll());            // /api/payment-methods
            await Task.WhenAll(product, benefits, mediaSections, paymentMethods);
            return Ok(new { product = product.Result, benefits = benefits.Result, mediaSections = mediaSections.Result, paymentMethods = paymentMethods.Result });
        }

        /// <summary>Mascotas (GPS multipropósito): reemplaza 4 llamadas.</summary>
        [HttpGet("pets")]
        public async Task<IActionResult> Pets()
        {
            var product = From<ProductsPetsController>(c => c.Get());                       // /api/pets-products
            var benefits = From<ProductsPetsBenefitController>(c => c.GetAll());            // /api/pets-products-benefits
            var mediaSections = From<ProductsPetsMediaSectionController>(c => c.GetAll());  // /api/pets-products-media-sections
            var paymentMethods = From<PaymentMethodController>(c => c.GetAll());            // /api/payment-methods
            await Task.WhenAll(product, benefits, mediaSections, paymentMethods);
            return Ok(new { product = product.Result, benefits = benefits.Result, mediaSections = mediaSections.Result, paymentMethods = paymentMethods.Result });
        }

        /// <summary>
        /// Ejecuta la acción GET de otro controlador (con sus mismas dependencias y contexto)
        /// y devuelve el cuerpo de su respuesta 200; si responde otra cosa (404, etc.), null.
        /// </summary>
        private async Task<object?> From<TController>(Func<TController, Task<IActionResult>> action) where TController : ControllerBase
        {
            var controller = ActivatorUtilities.CreateInstance<TController>(HttpContext.RequestServices);
            controller.ControllerContext = ControllerContext;

            var result = await action(controller);
            return result is ObjectResult { StatusCode: null or StatusCodes.Status200OK } ok ? ok.Value : null;
        }
    }
}
