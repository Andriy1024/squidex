﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Migrations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using Squidex.Domain.Apps.Core;
using Squidex.Domain.Apps.Core.Apps;
using Squidex.Domain.Apps.Core.Apps.Json;
using Squidex.Domain.Apps.Core.Contents;
using Squidex.Domain.Apps.Core.Contents.Json;
using Squidex.Domain.Apps.Core.Rules;
using Squidex.Domain.Apps.Core.Rules.Json;
using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Domain.Apps.Core.Schemas.Json;
using Squidex.Domain.Apps.Events;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Json;
using Squidex.Infrastructure.Json.Newtonsoft;
using Squidex.Infrastructure.Json.Objects;
using Squidex.Infrastructure.Queries;
using Squidex.Infrastructure.Reflection;

namespace Squidex.Config.Domain
{
    public static class SerializationServices
    {
        private static JsonSerializerSettings ConfigureJson(JsonSerializerSettings settings, TypeNameHandling typeNameHandling)
        {
            settings.Converters.Add(new StringEnumConverter());

            settings.ContractResolver = new ConverterContractResolver(
                new ContentFieldDataConverter(),
                new EnvelopeHeadersConverter(),
                new JsonValueConverter(),
                new StringEnumConverter(),
                new SurrogateConverter<AppClients, AppClientsSurrogate>(),
                new SurrogateConverter<AppContributors, AppContributorsSurrogate>(),
                new SurrogateConverter<AppPatterns, AppPatternsSurrogate>(),
                new SurrogateConverter<ClaimsPrincipal, ClaimsPrinicpalSurrogate>(),
                new SurrogateConverter<FilterNode<IJsonValue>, JsonFilterSurrogate>(),
                new SurrogateConverter<LanguageConfig, LanguageConfigSurrogate>(),
                new SurrogateConverter<LanguagesConfig, LanguagesConfigSurrogate>(),
                new SurrogateConverter<Roles, RolesSurrogate>(),
                new SurrogateConverter<Rule, RuleSorrgate>(),
                new SurrogateConverter<Schema, SchemaSurrogate>(),
                new SurrogateConverter<Workflows, WorkflowsSurrogate>(),
                new SurrogateConverter<WorkflowStep, WorkflowStepSurrogate>(),
                new SurrogateConverter<WorkflowTransition, WorkflowTransitionSurrogate>(),
                new WriteonlyGeoJsonConverter());

            settings.NullValueHandling = NullValueHandling.Ignore;

            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            settings.DateParseHandling = DateParseHandling.None;

            settings.TypeNameHandling = typeNameHandling;

            settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            return settings;
        }

        public static IServiceCollection AddSquidexSerializers(this IServiceCollection services)
        {
            services.AddSingletonAs<AutoAssembyTypeProvider<SquidexCoreModel>>()
                .As<ITypeProvider>();

            services.AddSingletonAs<AutoAssembyTypeProvider<SquidexEvents>>()
                .As<ITypeProvider>();

            services.AddSingletonAs<AutoAssembyTypeProvider<SquidexInfrastructure>>()
                .As<ITypeProvider>();

            services.AddSingletonAs<AutoAssembyTypeProvider<SquidexMigrations>>()
                .As<ITypeProvider>();

            services.AddSingletonAs<FieldRegistry>()
                .As<ITypeProvider>();

            services.AddSingletonAs<NewtonsoftJsonSerializer>()
                .As<IJsonSerializer>();

            services.AddSingletonAs<SerializationInitializer>()
                .AsSelf();

            services.AddSingletonAs<TypeNameRegistry>()
                .AsSelf();

            services.AddSingletonAs(c => JsonSerializer.Create(c.GetRequiredService<JsonSerializerSettings>()))
                .AsSelf();

            services.AddSingletonAs(c =>
                {
                    var serializerSettings = ConfigureJson(new JsonSerializerSettings(), TypeNameHandling.Auto);

                    var typeNameRegistry = c.GetService<TypeNameRegistry>();

                    if (typeNameRegistry != null)
                    {
                        serializerSettings.SerializationBinder = new TypeNameSerializationBinder(typeNameRegistry);
                    }

                    return serializerSettings;
                }).As<JsonSerializerSettings>();

            return services;
        }

        public static IMvcBuilder AddSquidexSerializers(this IMvcBuilder mvc)
        {
            mvc.AddNewtonsoftJson(options =>
            {
                options.AllowInputFormatterExceptionMessages = false;

                ConfigureJson(options.SerializerSettings, TypeNameHandling.None);
            });

            return mvc;
        }
    }
}
