using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace MreHunter.Api.Infrastructure
{
    /// <summary>
    /// Seguridad por defecto: toda acción que acepte un método distinto de GET/HEAD exige un JWT válido,
    /// salvo que la acción o el controlador tengan [AllowAnonymous].
    /// Así un endpoint nuevo de escritura no queda expuesto por olvidar el [Authorize].
    /// </summary>
    public sealed class RequireAuthForWritesConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            var isAnonymous =
                action.Attributes.OfType<IAllowAnonymous>().Any() ||
                action.Controller.Attributes.OfType<IAllowAnonymous>().Any();

            if (isAnonymous)
                return;

            var methods = action.Attributes
                .OfType<IActionHttpMethodProvider>()
                .SelectMany(a => a.HttpMethods)
                .ToList();

            var onlyReads = methods.Count > 0 && methods.All(m =>
                HttpMethods.IsGet(m) || HttpMethods.IsHead(m));

            if (onlyReads)
                return;

            foreach (var selector in action.Selectors)
                selector.EndpointMetadata.Add(new AuthorizeAttribute());
        }
    }
}
