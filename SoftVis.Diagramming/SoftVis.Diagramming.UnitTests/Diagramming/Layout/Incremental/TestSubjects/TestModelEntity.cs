﻿using Codartis.SoftVis.Modeling;
using Codartis.SoftVis.Modeling.Implementation;

namespace Codartis.SoftVis.Diagramming.UnitTests.Diagramming.Layout.Incremental.TestSubjects
{
    internal class TestModelEntity : ModelEntity
    {
        public TestModelEntity(string name = null)
            :base(name, ModelEntityType.Class, ModelEntityStereotype.None)
        {
        }

        public override int Priority => 0;
    }
}
