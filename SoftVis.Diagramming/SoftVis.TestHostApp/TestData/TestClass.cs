﻿using Codartis.SoftVis.Modeling;

namespace Codartis.SoftVis.TestHostApp.TestData
{
    internal class TestClass : TestModelEntity
    {
        public TestClass(string name, int size)
            : base(name, size, ModelEntityType.Class, ModelEntityStereotype.None)
        {
        }

        public override int Priority => 2;
    }
}
