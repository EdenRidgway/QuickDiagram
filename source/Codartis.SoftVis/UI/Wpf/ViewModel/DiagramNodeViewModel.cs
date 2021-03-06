﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Codartis.SoftVis.Diagramming.Definition;
using Codartis.SoftVis.Geometry;
using Codartis.SoftVis.Modeling.Definition;
using Codartis.Util.UI.Wpf;
using JetBrains.Annotations;

namespace Codartis.SoftVis.UI.Wpf.ViewModel
{
    /// <summary>
    /// View model for diagram nodes.
    /// </summary>
    public class DiagramNodeViewModel : DiagramShapeViewModelBase, IDiagramNodeUi
    {
        private string _name;
        private Size _headerSize;
        private Size _childrenAreaSize;
        private bool _hasChildren;

        public IDiagramNodeHeaderUi Header { get; }

        [NotNull] protected IRelatedNodeTypeProvider RelatedNodeTypeProvider { get; }
        [NotNull] public IWpfFocusTracker<IDiagramShapeUi> FocusTracker { get; }
        [NotNull] [ItemNotNull] public List<RelatedNodeCueViewModel> RelatedNodeCueViewModels { get; }

        public event Action<IDiagramNode, Size2D> HeaderSizeChanged;
        public event RelatedNodeMiniButtonEventHandler ShowRelatedNodesRequested;
        public event RelatedNodeMiniButtonEventHandler RelatedNodeSelectorRequested;
        public event Action<IDiagramNode> RemoveRequested;

        public DiagramNodeViewModel(
            [NotNull] IModelEventSource modelEventSource,
            [NotNull] IDiagramEventSource diagramEventSource,
            [NotNull] IDiagramNode diagramNode,
            [NotNull] IRelatedNodeTypeProvider relatedNodeTypeProvider,
            [NotNull] IWpfFocusTracker<IDiagramShapeUi> focusTracker,
            [NotNull] IDiagramNodeHeaderUi header)
            : base(modelEventSource, diagramEventSource, diagramNode)
        {
            RelatedNodeTypeProvider = relatedNodeTypeProvider;
            FocusTracker = focusTracker;
            RelatedNodeCueViewModels = CreateRelatedNodeCueViewModels();
            Header = header;

            SetDiagramNodeProperties(diagramNode);
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var relatedNodeCueViewModel in RelatedNodeCueViewModels)
                relatedNodeCueViewModel.Dispose();
        }

        public ModelNodeStereotype Stereotype => DiagramNode.ModelNode.Stereotype;
        public override string StereotypeName => Stereotype.Name;
        public IDiagramNode DiagramNode => (IDiagramNode)DiagramShape;
        [NotNull] public IModelNode ModelNode => DiagramNode.ModelNode;
        [NotNull] private DiagramNodeHeaderViewModel HeaderViewModel => (DiagramNodeHeaderViewModel)Header;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public Size HeaderSize
        {
            get { return _headerSize; }
            set
            {
                if (_headerSize != value)
                {
                    _headerSize = value;
                    OnPropertyChanged();

                    // This property binds to its control and propagates size changes to parent viewmodels.
                    HeaderSizeChanged?.Invoke(DiagramNode, _headerSize.FromWpf());
                }
            }
        }

        public Size ChildrenAreaSize
        {
            get { return _childrenAreaSize; }
            set
            {
                if (_childrenAreaSize != value)
                {
                    _childrenAreaSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasChildren
        {
            get { return _hasChildren; }
            set
            {
                if (_hasChildren != value)
                {
                    _hasChildren = value;
                    OnPropertyChanged();
                }
            }
        }

        public void Remove() => RemoveRequested?.Invoke(DiagramNode);

        public void ShowRelatedModelNodes(RelatedNodeMiniButtonViewModel ownerButton, IReadOnlyList<IModelNode> modelNodes)
            => ShowRelatedNodesRequested?.Invoke(ownerButton, modelNodes);

        public void ShowRelatedModelNodeSelector(RelatedNodeMiniButtonViewModel ownerButton, IReadOnlyList<IModelNode> modelNodes)
            => RelatedNodeSelectorRequested?.Invoke(ownerButton, modelNodes);

        public sealed override IDiagramShapeUi CloneForImageExport()
        {
            var clone = CreateInstance();
            SetPropertiesForImageExport(clone);
            return clone;
        }

        [NotNull]
        protected virtual DiagramNodeViewModel CreateInstance()
        {
            return new DiagramNodeViewModel(
                ModelEventSource,
                DiagramEventSource,
                DiagramNode,
                RelatedNodeTypeProvider,
                FocusTracker,
                Header);
        }

        private void SetPropertiesForImageExport([NotNull] DiagramNodeViewModel clone)
        {
            // For image export we must set those properties that are calculated on the normal UI.
            clone._headerSize = _headerSize;
        }

        public override IEnumerable<IMiniButton> CreateMiniButtons()
        {
            yield return new CloseMiniButtonViewModel(ModelEventSource, DiagramEventSource);

            foreach (var entityRelationType in GetRelatedNodeTypes())
                yield return new RelatedNodeMiniButtonViewModel(ModelEventSource, DiagramEventSource, entityRelationType);
        }

        [NotNull]
        private IEnumerable<RelatedNodeType> GetRelatedNodeTypes() => RelatedNodeTypeProvider.GetRelatedNodeTypes(ModelNode.Stereotype);

        public void Update([NotNull] IDiagramNode diagramNode)
        {
            SetDiagramShapeProperties(diagramNode);
            SetDiagramNodeProperties(diagramNode);
            UpdateHeader(diagramNode);
        }

        private void SetDiagramNodeProperties([NotNull] IDiagramNode diagramNode)
        {
            Name = diagramNode.ModelNode.Name;
            ChildrenAreaSize = diagramNode.ChildrenAreaSize.ToWpf();
            HasChildren = GetHasChildren(diagramNode);
            // Must NOT populate size from model because its value flows from the controls to the models.
        }

        /// <summary>
        /// Subtypes with a different header must override this method.
        /// </summary>
        protected virtual void UpdateHeader([NotNull] IDiagramNode diagramNode)
        {
            HeaderViewModel.Payload = diagramNode.ModelNode.Payload;
        }

        private static bool GetHasChildren([NotNull] IDiagramNode diagramNode)
        {
            return diagramNode.ChildrenAreaSize.IsDefined && diagramNode.ChildrenAreaSize != Size2D.Zero;
        }

        [NotNull]
        [ItemNotNull]
        private List<RelatedNodeCueViewModel> CreateRelatedNodeCueViewModels()
        {
            return GetRelatedNodeTypes()
                .Select(i => new RelatedNodeCueViewModel(ModelEventSource, DiagramEventSource, DiagramNode, i))
                .ToList();
        }
    }
}