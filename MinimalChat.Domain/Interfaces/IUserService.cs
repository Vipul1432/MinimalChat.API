using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Registers a new user asynchronously using the provided registration model.
        /// </summary>
        /// <param name="model">The registration data.</param>
        /// <returns>
        /// An ApiResponse containing the registration result, including a message and status code.
        /// </returns>
        Task<ApiResponse<RegistrationDto>> RegisterAsync(Registration model);

    }
}
