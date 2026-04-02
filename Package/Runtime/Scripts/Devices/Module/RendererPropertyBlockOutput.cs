using System;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class RendererPropertyBlockOutput : ColorDimmerOutputBase
    {
        [SerializeField] private Renderer[] targetRenderers = Array.Empty<Renderer>();
        [SerializeField] private string colorPropertyName = "_DmxColor";
        [SerializeField] private string dimmerPropertyName = "_DmxDimmer";

        private MaterialPropertyBlock[] _propertyBlocks = Array.Empty<MaterialPropertyBlock>();
        private int _colorPropertyId = -1;
        private int _dimmerPropertyId = -1;

        protected override void OnInitialize()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                var renderer = GetComponent<Renderer>();
                targetRenderers = renderer != null ? new[] { renderer } : Array.Empty<Renderer>();
            }

            _propertyBlocks = new MaterialPropertyBlock[targetRenderers.Length];
            _colorPropertyId = string.IsNullOrWhiteSpace(colorPropertyName) ? -1 : Shader.PropertyToID(colorPropertyName);
            _dimmerPropertyId = string.IsNullOrWhiteSpace(dimmerPropertyName) ? -1 : Shader.PropertyToID(dimmerPropertyName);

            for (var i = 0; i < _propertyBlocks.Length; i++)
            {
                _propertyBlocks[i] = new MaterialPropertyBlock();
            }

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

        public void Configure(Renderer[] renderers, string colorProperty, string dimmerProperty)
        {
            targetRenderers = renderers ?? Array.Empty<Renderer>();
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

                var propertyBlock = _propertyBlocks[i] ??= new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(propertyBlock);

                if (_colorPropertyId >= 0)
                {
                    propertyBlock.SetColor(_colorPropertyId, color);
                }

                if (_dimmerPropertyId >= 0)
                {
                    propertyBlock.SetFloat(_dimmerPropertyId, dimmer);
                }

                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
