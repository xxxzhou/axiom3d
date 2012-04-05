﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Axiom.Core;

namespace Axiom.Samples.ShaderSystem
{
    [Export(typeof(IPlugin))]
    public class Plugin : SamplePlugin
    {
        private ShaderSample sample;

        public override void Initialize()
        {
            sample = new ShaderSample();
            Name = sample.Metadata["Title"] + " Sample";
            AddSample(sample);
        }
    }
}
