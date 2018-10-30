﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class PackageAnalyzerAssemblyDependencyModel : DependencyModel
    {
        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.CodeInformation,
            expandedIcon: KnownMonikers.CodeInformation,
            unresolvedIcon: ManagedImageMonikers.CodeInformationWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.CodeInformationWarning);

        public override string ProviderType => PackageRuleHandler.ProviderTypeString;

        public PackageAnalyzerAssemblyDependencyModel(
            string path,
            string originalItemSpec,
            string name,
            ProjectTreeFlags flags,
            bool resolved,
            IImmutableDictionary<string, string> properties,
            IEnumerable<string> dependenciesIDs)
            : base(path, originalItemSpec, flags, resolved, isImplicit: false, properties: properties)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            Name = name;
            Caption = name;
            TopLevel = false;
            IconSet = s_iconSet;

            if (dependenciesIDs != null)
            {
                DependencyIDs = ImmutableArray.CreateRange(dependenciesIDs);
            }

            if (resolved)
            {
                Priority = Dependency.PackageAssemblyNodePriority;
            }
            else
            {
                Priority = Dependency.UnresolvedReferenceNodePriority;
            }
        }

        public override string Id => OriginalItemSpec;
    }
}
