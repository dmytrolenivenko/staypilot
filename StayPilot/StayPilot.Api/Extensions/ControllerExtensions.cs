using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Api.Extensions
{
    /// <summary>
    /// Turns a service response into an HTTP answer.
    /// One place decides the status code, so every endpoint answers the same way and no
    /// controller has to know which errors mean what.
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// 200 when nothing went wrong, 404 when the caller asked for something that does not
        /// exist, 400 for anything else. The response itself is the body either way, so the
        /// caller always gets the same shape back.
        /// </summary>
        public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, T response) where T : ResponseBase
        {
            if (response.Succeeded)
            {
                return controller.Ok(response);
            }

            var statusCode = response.Errors!.Any(x => x.IsNotFound())
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return controller.StatusCode(statusCode, response);
        }
    }
}
