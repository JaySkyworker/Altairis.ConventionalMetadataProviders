﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Altairis.ConventionalMetadataProviders {
    public class ConventionalValidationMetadataProvider : IValidationMetadataProvider {
        private const string AttributeNameSuffix = "Attribute";
        private ResourceManager _resourceManager;
        private Type _resourceType;

        public ConventionalValidationMetadataProvider() : this(typeof(Resources.DefaultValidationMessages)) {
        }

        public ConventionalValidationMetadataProvider(Type resourceType) {
            _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            _resourceManager = new ResourceManager(resourceType);
        }

        public void CreateValidationMetadata(ValidationMetadataProviderContext context) {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Add Required attribute to value types to simplify localization
            if (context.Key.ModelType.GetTypeInfo().IsValueType && !context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>().Any()) {
                context.ValidationMetadata.ValidatorMetadata.Add(new RequiredAttribute());
            }

            foreach (var attribute in context.ValidationMetadata.ValidatorMetadata) {
                var validationAttribute = attribute as ValidationAttribute;
                if (validationAttribute == null) continue; // Not a validation attribute

                // Do nothing if custom error message or localization options are specified
                if (!(string.IsNullOrWhiteSpace(validationAttribute.ErrorMessage) || attribute is DataTypeAttribute)) continue;
                if (!string.IsNullOrWhiteSpace(validationAttribute.ErrorMessageResourceName) && validationAttribute.ErrorMessageResourceType != null) continue;

                // Get attribute name without the "Attribute" suffix
                var attributeName = validationAttribute.GetType().Name;
                if (attributeName.EndsWith(AttributeNameSuffix, StringComparison.Ordinal)) attributeName = attributeName.Substring(0, attributeName.Length - AttributeNameSuffix.Length);

                // Link to resource if exists
                if (_resourceManager.GetString(attributeName) != null) {
                    validationAttribute.ErrorMessageResourceType = _resourceType;
                    validationAttribute.ErrorMessageResourceName = attributeName;
                    validationAttribute.ErrorMessage = null;
                } else {
                    validationAttribute.ErrorMessage = $"Missing resource key '{attributeName}'.";
                }

            }
        }

    }
}
