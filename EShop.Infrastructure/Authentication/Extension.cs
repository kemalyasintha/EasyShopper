using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace EShop.Infrastructure.Authentication
{
    public static class Extension
    {
        public static IServiceCollection AddJwt(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var options = new JwtOptions();
            configuration.GetSection("jwt").Bind(options);

            if (string.IsNullOrWhiteSpace(options.SecretKey))
            {
                throw new InvalidOperationException(
                    "JWT SecretKey is missing. Configure it using User Secrets or an environment variable.");
            }

            if (string.IsNullOrWhiteSpace(options.Issuer))
            {
                throw new InvalidOperationException("JWT Issuer is missing.");
            }

            if (string.IsNullOrWhiteSpace(options.Audience))
            {
                throw new InvalidOperationException("JWT Audience is missing.");
            }

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(options.SecretKey));

            services.AddSingleton<IAuthenticationHandler, AuthenticationHandler>();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwt =>
                {
                    // Suitable for the current local development environment.
                    // Production deployments must use HTTPS.
                    jwt.RequireHttpsMetadata = false;
                    jwt.SaveToken = true;

                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,

                        ValidateIssuer = true,
                        ValidIssuer = options.Issuer,

                        ValidateAudience = true,
                        ValidAudience = options.Audience,

                        ValidateLifetime = true,
                        RequireExpirationTime = true,

                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            return services;
        }
    }
}