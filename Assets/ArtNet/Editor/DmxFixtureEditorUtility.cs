using System.Collections.Generic;
using System.Linq;
using ArtNet.Devices.Modular;
using UnityEditor;
using UnityEngine;

namespace ArtNet.Editor
{
    internal static class DmxFixtureEditorUtility
    {
        public static DmxFixture[] FindFixturesInScene()
        {
            return Object.FindObjectsOfType<DmxFixture>(true)
                .Where(fixture => !EditorUtility.IsPersistent(fixture))
                .OrderBy(fixture => BuildHierarchyPath(fixture.transform))
                .ToArray();
        }

        public static string BuildGroupKey(DmxFixture fixture)
        {
            if (!string.IsNullOrWhiteSpace(fixture.GroupId))
            {
                return fixture.GroupId;
            }

            var parent = fixture.transform.parent;
            return parent != null ? $"Parent:{parent.GetInstanceID()}" : "Parent:<root>";
        }

        public static string BuildGroupLabel(DmxFixture fixture)
        {
            if (!string.IsNullOrWhiteSpace(fixture.GroupId))
            {
                return fixture.GroupId;
            }

            var parent = fixture.transform.parent;
            return parent != null ? parent.name : "<root>";
        }

        public static string BuildSignature(DmxFixture fixture)
        {
            return string.Join("|", fixture.Modules
                .Where(entry => entry.module != null)
                .Select(entry =>
                {
                    var channels = string.Join(",", entry.module.GetChannelDescriptors().Select(d => d.Name));
                    return $"{entry.module.GetType().Name}({channels})";
                }));
        }

        public static void PatchSequentially(IReadOnlyList<DmxFixture> fixtures, ushort universe, ushort startAddress)
        {
            var ordered = fixtures
                .OrderBy(fixture => BuildHierarchyPath(fixture.transform))
                .ToArray();

            var currentUniverse = (int)universe;
            var nextAddress = (int)startAddress;
            foreach (var fixture in ordered)
            {
                var footprint = Mathf.Max(fixture.ChannelFootprint, 1);
                if (nextAddress + footprint > 512)
                {
                    currentUniverse += 1;
                    nextAddress = 0;
                }

                Undo.RecordObject(fixture, "Patch DMX Fixtures");
                fixture.SetAddressPatch((ushort)currentUniverse, (ushort)nextAddress);
                EditorUtility.SetDirty(fixture);
                nextAddress += footprint;
            }
        }

        private static string BuildHierarchyPath(Transform target)
        {
            var stack = new Stack<string>();
            var current = target;
            while (current != null)
            {
                stack.Push($"{current.GetSiblingIndex():D4}_{current.name}");
                current = current.parent;
            }

            return string.Join("/", stack);
        }
    }
}
