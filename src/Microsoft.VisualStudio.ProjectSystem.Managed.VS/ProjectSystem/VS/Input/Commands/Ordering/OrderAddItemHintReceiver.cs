﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [Export(typeof(IProjectChangeHintReceiver))]
    [Export(typeof(OrderAddItemHintReceiver))]
    [ProjectChangeHintKind(ProjectChangeFileSystemEntityHint.AddedFileAsString)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class OrderAddItemHintReceiver : IProjectChangeHintReceiver
    {
        private readonly IProjectAccessor _accessor;

        private ImmutableHashSet<string> _previousIncludes = ImmutableHashSet<string>.Empty;
        private OrderingMoveAction _action = OrderingMoveAction.NoOp;
        private IProjectTree _target = null;
        private bool _isHinting = false;

        [ImportingConstructor]
        public OrderAddItemHintReceiver(IProjectAccessor accessor)
        {
            Requires.NotNull(accessor, nameof(accessor));

            _accessor = accessor;
        }

        public async Task HintedAsync(IImmutableDictionary<Guid, IImmutableSet<IProjectChangeHint>> hints)
        {
            if (CanMove() && !_previousIncludes.IsEmpty && hints.ContainsKey(ProjectChangeFileSystemEntityHint.AddedFile))
            {
                IProjectChangeHint hint = hints[ProjectChangeFileSystemEntityHint.AddedFile].First();
                ConfiguredProject configuredProject = hint.UnconfiguredProject.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject;
                await OrderingHelper.Move(configuredProject, _accessor, _previousIncludes, _target, _action).ConfigureAwait(false);
            }

            // Reset everything because we are done.
            // We need to make sure these are all reset so we can listen for changes again.
            Reset();
        }

        public async Task HintingAsync(IProjectChangeHint hint)
        {
            // This will only be called once even if you are adding multiple files from say, e.g. add existing item dialog
            // However we check to see if we captured the previous includes for sanity to ensure it only gets set once.
            if (CanMove() && _previousIncludes.IsEmpty)
            {
                ConfiguredProject configuredProject = hint.UnconfiguredProject.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject;

                _previousIncludes = await OrderingHelper.GetAllEvaluatedIncludes(configuredProject, _accessor).ConfigureAwait(false);

                _isHinting = true;
            }
        }

        /// <summary>
        /// When the task runs, if the receiver picks up that we will be adding an item, it will capture the MSBuild project's includes.
        /// If any items were added as a result of the task running, the hint receiver will perform the specified action on those items.
        /// </summary>
        public async Task Capture(OrderingMoveAction action, IProjectTree target, Func<Task> task)
        {
            Requires.NotNull(target, nameof(target));
            Requires.NotNull(task, nameof(task));

            _action = action;
            _target = target;
            await task().ConfigureAwait(false);

            // We need to be sure we are not hinting before we reset, otherwise everything would get reset before HintedAsync gets called.
            // This is for sanity.
            if (!_isHinting)
            {
                Reset();
            }
        }

        private void Reset()
        {
            _action = OrderingMoveAction.NoOp;
            _target = null;
            _previousIncludes = ImmutableHashSet<string>.Empty;
            _isHinting = false;
        }

        private bool CanMove()
        {
            return _action != OrderingMoveAction.NoOp && _target != null;
        }
    }
}
