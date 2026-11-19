using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OutputCaching;

namespace MreHunter.Api.Infrastructure
{
    public static class CacheTags
    {
        /// <summary>Etiqueta común de todas las respuestas GET cacheadas del CMS.</summary>
        public const string Cms = "cms";
    }

    /// <summary>
    /// Después de cualquier escritura (POST/PUT/PATCH/DELETE) hecha por un usuario autenticado
    /// se descarta la caché, para que el sitio muestre el cambio de inmediato.
    /// Las escrituras anónimas (contacto, newsletter, empleo) no modifican contenido público y no la invalidan.
    /// </summary>
    public sealed class OutputCacheEvictionFilter : IAsyncActionFilter
    {
        private readonly IOutputCacheStore _store;

        public OutputCacheEvictionFilter(IOutputCacheStore store) => _store = store;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();

            var http = context.HttpContext;
            var method = http.Request.Method;

            if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
                return;

            if (http.User.Identity?.IsAuthenticated != true)
                return;

            await _store.EvictByTagAsync(CacheTags.Cms, CancellationToken.None);
        }
    }
}
