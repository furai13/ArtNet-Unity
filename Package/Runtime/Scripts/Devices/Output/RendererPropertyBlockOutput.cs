using System;
using UnityEngine;

namespace ArtNet.Devices.Modular.Output
{
    public class RendererPropertyBlockOutput : ColorDimmerOutputBase
    {
        [SerializeField] private Renderer[] targetRenderers = Array.Empty<Renderer>();
        [SerializeField] private int[] targetMaterialIndices = Array.Empty<int>();
        [SerializeField] private string colorPropertyName = "_DmxColor";
        [SerializeField] private string dimmerPropertyName = "_DmxDimmer";

        private MaterialPropertyBlock[][] _propertyBlocksPerMaterial = Array.Empty<MaterialPropertyBlock[]>();
        private int _colorPropertyId = -1;
        private int _dimmerPropertyId = -1;

        protected override void OnInitialize()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                var renderer = GetComponent<Renderer>();
                targetRenderers = renderer != null ? new[] { renderer } : Array.Empty<Renderer>();
            }

            _propertyBlocksPerMaterial = new MaterialPropertyBlock[targetRenderers.Length][];
            _colorPropertyId = string.IsNullOrWhiteSpace(colorPropertyName) ? -1 : Shader.PropertyToID(colorPropertyName);
            _dimmerPropertyId = string.IsNullOrWhiteSpace(dimmerPropertyName) ? -1 : Shader.PropertyToID(dimmerPropertyName);

            InitializeColorDimmerOutput();
        }

        public override void Apply()
        {
            RefreshColorDimmerOutput();
        }

        public override void Tick(float deltaTime)
        {
            UpdateColorDimmerOutput(deltaTime);
        }

        public void Configure(Renderer[] renderers, string colorProperty, string dimmerProperty, int[] materialIndices = null)
        {
            targetRenderers = renderers ?? Array.Empty<Renderer>();
            targetMaterialIndices = materialIndices ?? Array.Empty<int>();
            colorPropertyName = colorProperty ?? string.Empty;
            dimmerPropertyName = dimmerProperty ?? string.Empty;
        }

        protected override void ApplyColorDimmer(Color color, float dimmer)
        {
            for (var i = 0; i < targetRenderers.Length; i++)
            {
                var targetRenderer = targetRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                var sharedMaterials = targetRenderer.sharedMaterials;
                var materialCount = Mathf.Max(sharedMaterials?.Length ?? 0, 1);
                var propertyBlocks = _propertyBlocksPerMaterial[i];
                if (propertyBlocks == null || propertyBlocks.Length != materialCount)
                {
                    propertyBlocks = new MaterialPropertyBlock[materialCount];
                    _propertyBlocksPerMaterial[i] = propertyBlocks;
                }

                if (targetMaterialIndices == null || targetMaterialIndices.Length == 0)
                {
                    for (var materialIndex = 0; materialIndex < materialCount; materialIndex++)
                    {
                        ApplyToMaterialSlot(targetRenderer, propertyBlocks, materialIndex, materialCount, color, dimmer);
                    }
                    continue;
                }

                for (var targetIndex = 0; targetIndex < targetMaterialIndices.Length; targetIndex++)
                {
                    var materialIndex = targetMaterialIndices[targetIndex];
                    if (materialIndex < 0 || materialIndex >= materialCount)
                    {
                        continue;
                    }

                    ApplyToMaterialSlot(targetRenderer, propertyBlocks, materialIndex, materialCount, color, dimmer);
                }
            }
        }

        private void ApplyToMaterialSlot(Renderer targetRenderer, MaterialPropertyBlock[] propertyBlocks, int materialIndex, int materialCount, Color color, float dimmer)
        {
            var propertyBlock = propertyBlocks[materialIndex] ??= new MaterialPropertyBlock();
            var useRendererWideBlock = materialCount <= 1;
            if (useRendererWideBlock)
            {
                targetRenderer.GetPropertyBlock(propertyBlock);
            }
            else
            {
                targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);
            }

            if (_colorPropertyId >= 0)
            {
                propertyBlock.SetColor(_colorPropertyId, color);
            }

            if (_dimmerPropertyId >= 0)
            {
                propertyBlock.SetFloat(_dimmerPropertyId, dimmer);
            }

            if (useRendererWideBlock)
            {
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
            }
        }
    }
}
