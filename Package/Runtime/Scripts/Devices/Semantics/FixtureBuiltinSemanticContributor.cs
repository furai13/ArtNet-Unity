using ArtNet.Devices.Modular;
using ArtNet.Devices.Modular.Output;
using UnityEngine;

namespace ArtNet.Devices.Semantics
{
    [DisallowMultipleComponent]
    public sealed class FixtureBuiltinSemanticContributor : FixtureSemanticContributorBase
    {
        public override void Contribute(FixtureSemanticBinding binding, FixtureSemanticBuilder builder)
        {
            var fixture = binding.Fixture;
            if (fixture == null)
            {
                return;
            }

            foreach (var entry in fixture.Modules)
            {
                var module = entry.module;
                if (module == null)
                {
                    continue;
                }

                switch (module)
                {
                    case DimmerModule:
                        builder.AddCapability("dimmer");
                        break;
                    case ColorModule:
                        builder.AddCapability("color");
                        builder.AddCapability("rgb");
                        break;
                    case ColorWheelModule:
                        builder.AddCapability("color");
                        builder.AddCapability("color-wheel");
                        break;
                    case PanTiltModule:
                        builder.AddCapability("pan");
                        builder.AddCapability("tilt");
                        break;
                    case GoboModule:
                        builder.AddCapability("gobo");
                        break;
                    case StrobeModule:
                        builder.AddCapability("strobe");
                        break;
                }
            }

            foreach (var output in fixture.Outputs)
            {
                if (output == null)
                {
                    continue;
                }

                switch (output)
                {
                    case LightOutput lightOutput:
                        builder.AddCapability("light-output");
                        builder.AddCapability("beam-angle");
                        builder.AddProperty("output", "light");
                        builder.AddProperty("light-count", GetLightCount(lightOutput).ToString());
                        break;
                }
            }
        }

        private static int GetLightCount(LightOutput lightOutput)
        {
            var lights = lightOutput.GetComponentsInChildren<Light>(true);
            return lights?.Length ?? 0;
        }
    }
}
