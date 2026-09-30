using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using Object = UnityEngine.Object;

namespace kebinImports
{
    public partial class kebinImports
    {
        // The tools that make kebinAI useful for avatar work: an avatar overview, which bones a mesh uses (so PhysBones
        // land on bones, not meshes), blendshapes by name, transforms (scaling an avatar keeps its view point), moving,
        // renaming, deleting and duplicating objects, placing prefabs, and reading or changing any asset.
        internal static partial class AITools
        {
            private static void RegisterAvatarTools()
            {
                Register("get_avatar_info", "Overview of the avatars in the scene (or one avatar): its root object, whether it has a VRChat avatar descriptor, view position, scale, meshes and their materials, PhysBones with the bone each one starts at, colliders, contacts, expression menu and parameters, and where key humanoid bones are (head, chest, hands, ...). Call this first whenever the user talks about their avatar.", false,
                    Schema("object", "string", "Scene path of an avatar (or any object inside it). Omit for every avatar in the scene."),
                    a => GetAvatarInfo(a));
                Register("get_mesh_bones", "Which bones a skinned mesh (hair, hoodie, skirt, tail, ears, breasts, ...) is attached to. Lists the bones only that mesh uses and the first bone of each of their chains: that first bone is where a PhysBone goes to make the part move.", false,
                    Schema("object*", "string", "Scene path of the object with the SkinnedMeshRenderer, e.g. Avatar/Hair."),
                    a => GetMeshBones(a));
                Register("assign_material", "Put a material on a mesh. Mesh renderers can have several material slots; this replaces one slot and keeps the others.", true,
                    Schema("object*", "string", "Scene path of the object with the renderer, e.g. MyAvatar/Body.", "material*", "string", "Asset path of the material (find it with list_assets 't:Material name').", "slot", "integer", "Which slot to replace, from 0. Omit to replace slot 0 when there is only one slot; with several slots the result lists them so you can pick."),
                    a => AssignMaterial(a));
                Register("get_blendshapes", "List blendshapes (shape keys) with their current values (0-100), on one mesh or, without object, on every mesh in the scene's avatars.", false,
                    Schema("object", "string", "Scene path of a mesh (e.g. MyAvatar/Body) or of an avatar to search all its meshes. Omit to search every mesh in the scene.", "filter", "string", "Only blendshapes whose name contains this text."),
                    a => GetBlendshapes(a));
                Register("set_blendshape", "Set a blendshape (shape key) on a mesh by name, 0-100.", true,
                    Schema("object*", "string", "Scene path of the object with the SkinnedMeshRenderer.", "name*", "string", "Blendshape name (case-insensitive).", "value*", "number", "Weight, usually 0-100."),
                    a => SetBlendshape(a));
                Register("set_transform", "Move, rotate or scale a scene object. Use scale_by to scale relative to the current size (1.5 = 50% bigger). Scaling an avatar's root also scales its descriptor's view position so the viewpoint stays at the eyes.", true,
                    Schema("object*", "string", "Scene path of the object.", "position", "object", "{\"x\",\"y\",\"z\"} local position.", "rotation", "object", "{\"x\",\"y\",\"z\"} local rotation in degrees.", "scale", "object", "{\"x\",\"y\",\"z\"} local scale, or a number for uniform scale.", "scale_by", "number", "Multiply the current scale by this factor."),
                    a => SetTransform(a));
                Register("modify_gameobject", "Rename a scene object, move it under another object (or to the top of the scene), or change its tag or layer.", true,
                    Schema("object*", "string", "Scene path of the object.", "name", "string", "New name.", "parent", "string", "Scene path of the new parent, or \"\" for the top level.", "tag", "string", "Tag name.", "layer", "string", "Layer name."),
                    a => ModifyGameObject(a));
                Register("delete_gameobject", "Delete a scene object and everything under it.", true,
                    Schema("object*", "string", "Scene path of the object."),
                    a => DeleteGameObject(a));
                Register("duplicate_gameobject", "Duplicate a scene object (with its children), next to the original.", true,
                    Schema("object*", "string", "Scene path of the object.", "name", "string", "Name for the copy."),
                    a => DuplicateGameObject(a));
                Register("instantiate_prefab", "Place a prefab or model asset into the scene, optionally under a parent object.", true,
                    Schema("path*", "string", "Asset path of the prefab or model, e.g. Assets/MyAvatar/Avatar.prefab.", "parent", "string", "Scene path of the parent object."),
                    a => InstantiatePrefab(a));
                Register("select_objects", "Select scene objects or assets in the editor and frame them in the Scene view, to show the user something.", false,
                    Schema("objects*", "array:string", "Scene paths or asset paths."),
                    a => SelectObjects(a));
                Register("find_component_types", "Search the component types that can be added (from Unity, the VRChat SDK and installed tools), e.g. 'physbone', 'contact', 'constraint', 'descriptor'.", false,
                    Schema("query*", "string", "Part of the type name.", "limit", "integer", "Maximum results (default 30)."),
                    a => FindComponentTypes(a));
                Register("get_asset_properties", "Read the serialized properties of any asset (VRChat expression menus and parameters, animator controllers, physic materials, ScriptableObjects, ...). For materials use get_material.", false,
                    Schema("path*", "string", "Asset path.", "type", "string", "Type of the sub-asset to read when the file holds several.", "filter", "string", "Only properties whose path contains this text."),
                    a => GetAssetProperties(a));
                Register("set_asset_property", "Set one serialized property on any asset. Read it first with get_asset_properties to learn property paths and value shapes (same value format as set_component_property).", true,
                    Schema("path*", "string", "Asset path.", "type", "string", "Type of the sub-asset when the file holds several.", "property_path*", "string", "Serialized property path.", "value*", "any", "The new value."),
                    a => SetAssetProperty(a));
                Register("create_asset", "Create a new asset of a ScriptableObject type, e.g. VRCExpressionsMenu or VRCExpressionParameters, or an AnimatorController (.controller) or AnimationClip (.anim).", true,
                    Schema("path*", "string", "Asset path with extension (.asset, .controller, .anim).", "type*", "string", "Type name, e.g. VRCExpressionsMenu."),
                    a => CreateAsset(a));
            }

            // ---------------------------------------------------------------- type names
            // "VRC Avatar Descriptor", "vrchat_physbone", "PhysBone" -> comparable keys.
            private static string TypeKey(string name)
            {
                string k = new string((name ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
                if (k.StartsWith("vrchat")) k = k.Substring(6);
                else if (k.StartsWith("vrc")) k = k.Substring(3);
                if (k.EndsWith("component") && k.Length > 9) k = k.Substring(0, k.Length - 9);
                return k;
            }
            private static List<Type> componentTypes;
            private static List<Type> ComponentTypes()
            {
                if (componentTypes != null) return componentTypes;
                componentTypes = TypeCache.GetTypesDerivedFrom<Component>().Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && t.IsPublic && !t.Namespace.StartsWithSafe("UnityEditor")).ToList();
                return componentTypes;
            }
            // Finds a component type by exact name, then by a forgiving match ("avatar descriptor" -> VRCAvatarDescriptor).
            private static Type ResolveComponentType(string name, out List<string> suggestions)
            {
                suggestions = new List<string>();
                Type t = FindType(name, typeof(Component));
                if (t != null) return t;
                string key = TypeKey(name);
                if (key == "descriptor") key = "avatardescriptor";
                List<Type> all = ComponentTypes();
                List<Type> exact = all.Where(x => TypeKey(x.Name) == key).ToList();
                if (exact.Count >= 1) return exact.OrderBy(x => x.Namespace != null && x.Namespace.StartsWith("VRC") ? 0 : 1).First();
                List<Type> contains = all.Where(x => key.Length >= 4 && (TypeKey(x.Name).Contains(key) || key.Contains(TypeKey(x.Name)) && TypeKey(x.Name).Length >= 6)).ToList();
                if (contains.Count == 1) return contains[0];
                suggestions = contains.Select(x => x.Name).Distinct().Take(10).ToList();
                return null;
            }
            private static string FindComponentTypes(JSONNode a)
            {
                string key = TypeKey(Require(a, "query"));
                int limit = Int(a, "limit", 30);
                List<string> hits = ComponentTypes().Where(t => TypeKey(t.Name).Contains(key)).OrderBy(t => t.Name.Length).Select(t => t.Name + "  [" + t.FullName + "]").Distinct().Take(limit).ToList();
                return hits.Count == 0 ? "No component types match '" + Str(a, "query") + "'." : string.Join("\n", hits);
            }

            // ---------------------------------------------------------------- avatars
            private static bool IsDescriptor(Component c) => c != null && c.GetType().Name == "VRCAvatarDescriptor";
            // The avatar an object belongs to: the nearest ancestor with an avatar descriptor, else the top-level object.
            private static GameObject AvatarRootOf(GameObject go)
            {
                for (Transform t = go.transform; t != null; t = t.parent) if (t.GetComponents<Component>().Any(IsDescriptor)) return t.gameObject;
                return go.transform.root.gameObject;
            }
            private static IEnumerable<GameObject> SceneAvatars()
            {
                foreach (GameObject root in SceneRoots())
                {
                    List<GameObject> withDescriptor = root.GetComponentsInChildren<Component>(true).Where(IsDescriptor).Select(c => c.gameObject).Distinct().ToList();
                    if (withDescriptor.Count > 0) { foreach (GameObject g in withDescriptor) yield return g; continue; }
                    // Not set up for VRChat yet: a top-level object with an Animator and skinned meshes is an avatar too.
                    if (root.GetComponent<Animator>() != null && root.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) yield return root;
                }
            }
            // One line per avatar for the system prompt, so the model knows the names without a tool call.
            public static string DescribeAvatarsForPrompt()
            {
                List<string> parts = new List<string>();
                foreach (GameObject av in SceneAvatars().Take(6))
                {
                    bool desc = av.GetComponents<Component>().Any(IsDescriptor);
                    parts.Add("'" + PathOf(av.transform) + "'" + (desc ? "" : " (no VRChat avatar descriptor yet)"));
                }
                return parts.Count == 0 ? "none found" : string.Join(", ", parts);
            }
            private static string GetAvatarInfo(JSONNode a)
            {
                string path = Str(a, "object");
                List<GameObject> avatars = string.IsNullOrEmpty(path) ? SceneAvatars().ToList() : new List<GameObject> { AvatarRootOf(FindSceneObject(path)) };
                if (avatars.Count == 0) return "No avatars found in the open scene. (An avatar is an object with a VRChat avatar descriptor, or a top-level object with an Animator and skinned meshes.) Use get_hierarchy to look around.";
                StringBuilder sb = new StringBuilder();
                foreach (GameObject av in avatars)
                {
                    Transform root = av.transform;
                    Component descriptor = av.GetComponents<Component>().FirstOrDefault(IsDescriptor);
                    sb.AppendLine("Avatar '" + PathOf(root) + "'" + (av.activeInHierarchy ? "" : " (inactive)"));
                    sb.AppendLine("  scale " + Vec(root.localScale) + ", position " + Vec(root.position));
                    if (descriptor != null)
                    {
                        SerializedObject so = new SerializedObject(descriptor);
                        SerializedProperty view = so.FindProperty("ViewPosition");
                        sb.AppendLine("  VRChat avatar descriptor: yes" + (view != null ? ", view position " + Vec(view.vector3Value) : ""));
                        SerializedProperty menu = so.FindProperty("expressionsMenu"), prms = so.FindProperty("expressionParameters");
                        if (menu != null) sb.AppendLine("  expression menu: " + RefToJson(menu.objectReferenceValue) + ", expression parameters: " + (prms != null ? RefToJson(prms.objectReferenceValue).ToString() : "?"));
                    }
                    else sb.AppendLine("  VRChat avatar descriptor: NO (add a VRCAvatarDescriptor to '" + PathOf(root) + "' to make it uploadable)");
                    Animator animator = av.GetComponent<Animator>();
                    if (animator != null && animator.isHuman)
                    {
                        List<string> bones = new List<string>();
                        foreach (HumanBodyBones hb in new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Neck, HumanBodyBones.Head, HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
                        {
                            Transform b = animator.GetBoneTransform(hb);
                            if (b != null) bones.Add(hb + "=" + PathOf(b));
                        }
                        sb.AppendLine("  humanoid bones: " + string.Join("; ", bones));
                    }
                    else sb.AppendLine("  humanoid: " + (animator == null ? "no Animator" : "not humanoid"));
                    sb.AppendLine("  meshes:");
                    foreach (Renderer r in av.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r is ParticleSystemRenderer) continue;
                        sb.AppendLine("    " + PathOf(r.transform) + (r.gameObject.activeInHierarchy ? "" : " (hidden)") + ": " + string.Join(", ", r.sharedMaterials.Select(m => m != null ? m.name : "(no material)")));
                    }
                    List<Component> all = av.GetComponentsInChildren<Component>(true).Where(c => c != null).ToList();
                    List<Component> physbones = all.Where(c => c.GetType().Name == "VRCPhysBone").ToList();
                    sb.AppendLine("  PhysBones: " + (physbones.Count == 0 ? "none" : ""));
                    foreach (Component pb in physbones.Take(40))
                    {
                        SerializedObject so = new SerializedObject(pb);
                        Object rt = so.FindProperty("rootTransform") != null ? so.FindProperty("rootTransform").objectReferenceValue : null;
                        string starts = rt != null ? PathOf(((Transform)rt)) : PathOf(pb.transform);
                        sb.AppendLine("    on '" + PathOf(pb.transform) + "'" + (rt != null ? ", starts at '" + starts + "'" : "") + ", pull " + Fmt(so, "pull") + ", spring " + Fmt(so, "spring") + ", gravity " + Fmt(so, "gravity"));
                    }
                    if (physbones.Count > 40) sb.AppendLine("    … " + (physbones.Count - 40) + " more");
                    foreach (string kind in new[] { "VRCPhysBoneCollider", "VRCContactSender", "VRCContactReceiver" })
                    {
                        List<Component> list = all.Where(c => c.GetType().Name == kind).ToList();
                        if (list.Count > 0) sb.AppendLine("  " + kind + ": " + string.Join(", ", list.Take(20).Select(c => PathOf(c.transform))) + (list.Count > 20 ? " …" : ""));
                    }
                    int missing = av.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                    if (missing > 0) sb.AppendLine("  broken (missing script) components: " + missing);
                }
                return sb.ToString();
            }
            private static string Vec(Vector3 v) => "(" + v.x.ToString("0.###") + ", " + v.y.ToString("0.###") + ", " + v.z.ToString("0.###") + ")";
            private static string Fmt(SerializedObject so, string prop)
            {
                SerializedProperty p = so.FindProperty(prop);
                return p == null ? "?" : p.propertyType == SerializedPropertyType.Float ? p.floatValue.ToString("0.###") : PropToJson(p, 1).ToString();
            }

            // ---------------------------------------------------------------- meshes
            private static SkinnedMeshRenderer SkinnedMesh(GameObject go)
            {
                SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
                if (smr == null) throw new ArgumentException("'" + PathOf(go.transform) + "' has no SkinnedMeshRenderer (it isn't a skinned mesh). Meshes: " + string.Join(", ", AvatarRootOf(go).GetComponentsInChildren<SkinnedMeshRenderer>(true).Take(20).Select(s => PathOf(s.transform))));
                if (smr.sharedMesh == null) throw new ArgumentException("'" + PathOf(go.transform) + "' has no mesh assigned.");
                return smr;
            }
            private static HashSet<Transform> WeightedBones(SkinnedMeshRenderer smr)
            {
                HashSet<Transform> set = new HashSet<Transform>();
                Transform[] bones = smr.bones;
                foreach (BoneWeight w in smr.sharedMesh.boneWeights)
                {
                    if (w.weight0 > 0.01f && w.boneIndex0 < bones.Length && bones[w.boneIndex0] != null) set.Add(bones[w.boneIndex0]);
                    if (w.weight1 > 0.01f && w.boneIndex1 < bones.Length && bones[w.boneIndex1] != null) set.Add(bones[w.boneIndex1]);
                    if (w.weight2 > 0.01f && w.boneIndex2 < bones.Length && bones[w.boneIndex2] != null) set.Add(bones[w.boneIndex2]);
                    if (w.weight3 > 0.01f && w.boneIndex3 < bones.Length && bones[w.boneIndex3] != null) set.Add(bones[w.boneIndex3]);
                }
                return set;
            }
            private static string GetMeshBones(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                SkinnedMeshRenderer smr = SkinnedMesh(go);
                HashSet<Transform> mine = WeightedBones(smr);
                HashSet<Transform> others = new HashSet<Transform>();
                foreach (SkinnedMeshRenderer other in AvatarRootOf(go).GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (other == smr || other.sharedMesh == null) continue;
                    others.UnionWith(WeightedBones(other));
                }
                Animator animator = AvatarRootOf(go).GetComponent<Animator>();
                HashSet<Transform> humanoid = new HashSet<Transform>();
                if (animator != null && animator.isHuman)
                {
                    foreach (HumanBodyBones hb in Enum.GetValues(typeof(HumanBodyBones)))
                    {
                        if (hb == HumanBodyBones.LastBone) continue;
                        Transform b = animator.GetBoneTransform(hb);
                        if (b != null) humanoid.Add(b);
                    }
                }
                // Bones that move this part only: used by this mesh, not by the others, not part of the humanoid skeleton.
                List<Transform> own = mine.Where(b => !others.Contains(b) && !humanoid.Contains(b)).ToList();
                // Extra (non-humanoid) bones shared with other meshes still count, e.g. breasts used by body and shirt.
                List<Transform> extra = mine.Where(b => !humanoid.Contains(b)).ToList();
                List<Transform> chainStarts = extra.Where(b => b.parent == null || !extra.Contains(b.parent)).OrderBy(b => PathOf(b)).ToList();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("'" + PathOf(go.transform) + "' is weighted to " + mine.Count + " bones.");
                if (chainStarts.Count == 0) sb.AppendLine("It only follows the main humanoid skeleton, so it has no bones of its own to put a PhysBone on. (A PhysBone needs extra bones such as hair, ear, tail, skirt or breast bones.)");
                else
                {
                    sb.AppendLine("Chains of extra (non-humanoid) bones it uses, by their first bone; put a PhysBone on the first bone of a chain (or on a parent object with Root Transform set to it):");
                    foreach (Transform b in chainStarts.Take(40))
                    {
                        int length = b.GetComponentsInChildren<Transform>(true).Count(x => extra.Contains(x));
                        bool shared = !own.Contains(b);
                        Component mover = PhysBoneMoving(b);
                        sb.AppendLine("  " + PathOf(b) + "  (" + length + " bones" + (shared ? ", also used by other meshes" : "") + (mover != null ? ", ALREADY moved by the PhysBone on '" + PathOf(mover.transform) + "'" : "") + ")");
                    }
                }
                List<Transform> body = mine.Where(humanoid.Contains).ToList();
                if (body.Count > 0) sb.AppendLine("Humanoid bones it follows: " + string.Join(", ", body.Take(15).Select(b => b.name)) + (body.Count > 15 ? " …" : ""));
                return sb.ToString();
            }
            // The PhysBone that already moves this bone: one whose root (its Root Transform, or its own object) is the
            // bone or one of its parents. Null when nothing does.
            private static Component PhysBoneMoving(Transform bone)
            {
                for (Transform t = bone; t != null; t = t.parent)
                {
                    foreach (Component pb in t.root.GetComponentsInChildren<Component>(true).Where(c => c != null && c.GetType().Name == "VRCPhysBone"))
                    {
                        SerializedProperty rt = new SerializedObject(pb).FindProperty("rootTransform");
                        Transform root = rt != null && rt.objectReferenceValue != null ? (Transform)rt.objectReferenceValue : pb.transform;
                        if (root == t) return pb;
                    }
                }
                return null;
            }
            private static string GetBlendshapes(JSONNode a)
            {
                string filter = Str(a, "filter");
                GameObject target = string.IsNullOrEmpty(Str(a, "object")) ? null : FindSceneObject(Str(a, "object"));
                // No object, or an avatar / parent object rather than a mesh: search every mesh under it.
                if (target == null || target.GetComponent<SkinnedMeshRenderer>() == null)
                {
                    IEnumerable<SkinnedMeshRenderer> meshes = target == null ? SceneAvatars().SelectMany(av => av.GetComponentsInChildren<SkinnedMeshRenderer>(true)) : target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    StringBuilder all = new StringBuilder();
                    int found = 0;
                    foreach (SkinnedMeshRenderer r in meshes.Where(r => r.sharedMesh != null))
                    {
                        for (int i = 0; i < r.sharedMesh.blendShapeCount && found < 300; i++)
                        {
                            string n = r.sharedMesh.GetBlendShapeName(i);
                            if (!string.IsNullOrEmpty(filter) && n.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                            all.AppendLine(PathOf(r.transform) + ": " + n + " = " + r.GetBlendShapeWeight(i).ToString("0.##"));
                            found++;
                        }
                    }
                    return found == 0 ? "No mesh in the scene's avatars has a blendshape" + (string.IsNullOrEmpty(filter) ? "." : " matching '" + filter + "'. Try a shorter or different word (blendshape names are often Japanese or abbreviated, e.g. 'eye', 'まばたき', 'blink', 'wink').") : all.ToString();
                }
                SkinnedMeshRenderer smr = SkinnedMesh(target);
                Mesh mesh = smr.sharedMesh;
                StringBuilder sb = new StringBuilder();
                int shown = 0;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string n = mesh.GetBlendShapeName(i);
                    if (!string.IsNullOrEmpty(filter) && n.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    sb.AppendLine(n + " = " + smr.GetBlendShapeWeight(i).ToString("0.##"));
                    if (++shown >= 300) { sb.AppendLine("… use filter to narrow"); break; }
                }
                return mesh.blendShapeCount == 0 ? "This mesh has no blendshapes." : shown == 0 ? "No blendshape matches '" + filter + "'." : sb.ToString();
            }
            private static string SetBlendshape(JSONNode a)
            {
                SkinnedMeshRenderer smr = SkinnedMesh(FindSceneObject(Require(a, "object")));
                string name = Require(a, "name");
                Mesh mesh = smr.sharedMesh;
                int idx = mesh.GetBlendShapeIndex(name);
                for (int i = 0; idx < 0 && i < mesh.blendShapeCount; i++) if (mesh.GetBlendShapeName(i).Equals(name, StringComparison.OrdinalIgnoreCase)) idx = i;
                if (idx < 0)
                {
                    List<string> close = Enumerable.Range(0, mesh.blendShapeCount).Select(i => mesh.GetBlendShapeName(i)).Where(n => n.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0).Take(10).ToList();
                    throw new ArgumentException("No blendshape named '" + name + "'." + (close.Count > 0 ? " Similar: " + string.Join(", ", close) : " Use get_blendshapes to list them."));
                }
                float value = a["value"].AsFloat;
                Undo.RecordObject(smr, "Set blendshape");
                smr.SetBlendShapeWeight(idx, value);
                EditorUtility.SetDirty(smr);
                return "Set blendshape " + mesh.GetBlendShapeName(idx) + " on '" + PathOf(smr.transform) + "' to " + value.ToString("0.##") + ".";
            }

            private static string AssignMaterial(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                Renderer r = go.GetComponent<Renderer>();
                if (r == null) throw new ArgumentException("'" + PathOf(go.transform) + "' has no renderer, so it can't show a material.");
                string path = Require(a, "material");
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) throw new ArgumentException("No material at '" + path + "'. Use list_assets with 't:Material'.");
                Material[] mats = r.sharedMaterials;
                string Slots() => string.Join(", ", mats.Select((x, i) => "slot " + i + ": " + (x != null ? x.name : "empty")));
                int slot = Int(a, "slot", -1);
                if (slot < 0)
                {
                    if (mats.Length > 1) throw new ArgumentException("'" + PathOf(go.transform) + "' has " + mats.Length + " material slots (" + Slots() + "). Say which slot to replace.");
                    slot = 0;
                }
                if (slot >= mats.Length)
                {
                    if (slot > mats.Length) throw new ArgumentException("Slot " + slot + " doesn't exist; the slots are " + Slots() + ". Use slot " + mats.Length + " to add one more.");
                    Array.Resize(ref mats, mats.Length + 1);
                }
                Undo.RecordObject(r, "Assign material");
                mats[slot] = m;
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
                return "'" + PathOf(go.transform) + "' now has " + Slots() + ".";
            }

            // ---------------------------------------------------------------- objects
            private static string SetTransform(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                Transform t = go.transform;
                Vector3 oldScale = t.localScale;
                Undo.RecordObject(t, "Set transform");
                List<string> changed = new List<string>();
                if (a.HasKey("position")) { t.localPosition = ParseVector(a["position"], t.localPosition); changed.Add("position " + Vec(t.localPosition)); }
                if (a.HasKey("rotation")) { t.localEulerAngles = ParseVector(a["rotation"], t.localEulerAngles); changed.Add("rotation " + Vec(t.localEulerAngles)); }
                if (a.HasKey("scale"))
                {
                    t.localScale = a["scale"].IsNumber ? Vector3.one * a["scale"].AsFloat : (Vector3)ParseVector(a["scale"], t.localScale);
                    changed.Add("scale " + Vec(t.localScale));
                }
                if (a.HasKey("scale_by"))
                {
                    float f = a["scale_by"].AsFloat;
                    if (f <= 0) throw new ArgumentException("scale_by must be greater than 0.");
                    t.localScale = t.localScale * f;
                    changed.Add("scale " + Vec(t.localScale));
                }
                if (changed.Count == 0) throw new ArgumentException("Nothing to change: pass position, rotation, scale or scale_by.");
                EditorUtility.SetDirty(t);
                // The avatar descriptor's view position is stored unscaled; keep it at the eyes when the avatar is resized.
                Component descriptor = go.GetComponents<Component>().FirstOrDefault(IsDescriptor);
                if (descriptor != null && t.localScale != oldScale && oldScale.y != 0)
                {
                    SerializedObject so = new SerializedObject(descriptor);
                    SerializedProperty view = so.FindProperty("ViewPosition");
                    if (view != null)
                    {
                        Vector3 ratio = new Vector3(oldScale.x != 0 ? t.localScale.x / oldScale.x : 1, t.localScale.y / oldScale.y, oldScale.z != 0 ? t.localScale.z / oldScale.z : 1);
                        view.vector3Value = Vector3.Scale(view.vector3Value, ratio);
                        so.ApplyModifiedProperties();
                        changed.Add("view position " + Vec(view.vector3Value) + " to match");
                    }
                }
                return "Set " + string.Join(", ", changed) + " on '" + PathOf(t) + "'.";
            }
            private static string ModifyGameObject(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                List<string> changed = new List<string>();
                Undo.RecordObject(go, "Modify object");
                string name = Str(a, "name");
                if (!string.IsNullOrEmpty(name)) { go.name = name; changed.Add("renamed to " + name); }
                if (a.HasKey("parent"))
                {
                    string parent = Str(a, "parent", "");
                    Transform p = string.IsNullOrEmpty(parent) ? null : FindSceneObject(parent).transform;
                    if (p != null && (p == go.transform || p.IsChildOf(go.transform))) throw new ArgumentException("An object can't be moved under itself.");
                    Undo.SetTransformParent(go.transform, p, "Move object");
                    changed.Add(p == null ? "moved to the top level" : "moved under '" + PathOf(p) + "'");
                }
                string tag = Str(a, "tag");
                if (!string.IsNullOrEmpty(tag)) { go.tag = tag; changed.Add("tag " + tag); }
                string layer = Str(a, "layer");
                if (!string.IsNullOrEmpty(layer))
                {
                    int l = LayerMask.NameToLayer(layer);
                    if (l < 0) throw new ArgumentException("No layer named '" + layer + "'.");
                    go.layer = l;
                    changed.Add("layer " + layer);
                }
                if (changed.Count == 0) throw new ArgumentException("Nothing to change: pass name, parent, tag or layer.");
                EditorUtility.SetDirty(go);
                return "'" + PathOf(go.transform) + "': " + string.Join(", ", changed) + ".";
            }
            private static string DeleteGameObject(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                string path = PathOf(go.transform);
                if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                {
                    // Objects inside a prefab can only be removed by unpacking it, which is a bigger change than asked for.
                    throw new ArgumentException("'" + path + "' is part of a prefab, so it can't be deleted on its own. Disable it with set_active instead, or ask the user whether to unpack the prefab first.");
                }
                Undo.DestroyObjectImmediate(go);
                return "Deleted '" + path + "'.";
            }
            private static string DuplicateGameObject(JSONNode a)
            {
                GameObject go = FindSceneObject(Require(a, "object"));
                GameObject copy;
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                {
                    copy = (GameObject)PrefabUtility.InstantiatePrefab(PrefabUtility.GetCorrespondingObjectFromSource(go), go.transform.parent);
                    PrefabUtility.SetPropertyModifications(copy, PrefabUtility.GetPropertyModifications(go));
                }
                else copy = Object.Instantiate(go, go.transform.parent);
                copy.transform.SetSiblingIndex(go.transform.GetSiblingIndex() + 1);
                copy.transform.localPosition = go.transform.localPosition;
                copy.transform.localRotation = go.transform.localRotation;
                copy.transform.localScale = go.transform.localScale;
                copy.name = Str(a, "name") ?? GameObjectUtility.GetUniqueNameForSibling(go.transform.parent, go.name);
                Undo.RegisterCreatedObjectUndo(copy, "Duplicate " + go.name);
                return "Duplicated '" + PathOf(go.transform) + "' as '" + PathOf(copy.transform) + "'.";
            }
            private static string InstantiatePrefab(JSONNode a)
            {
                string path = Require(a, "path");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new ArgumentException("No prefab or model at '" + path + "'. Use list_assets with 't:Prefab' or 't:Model'.");
                string parent = Str(a, "parent");
                Transform p = string.IsNullOrEmpty(parent) ? null : FindSceneObject(parent).transform;
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, p);
                Undo.RegisterCreatedObjectUndo(go, "Place " + prefab.name);
                Selection.activeGameObject = go;
                return "Placed '" + prefab.name + "' in the scene as '" + PathOf(go.transform) + "'.";
            }
            private static string SelectObjects(JSONNode a)
            {
                List<Object> objs = new List<Object>();
                foreach (JSONNode n in a["objects"].Children)
                {
                    string p = n.Value;
                    if (p.StartsWith("Assets/") || p.StartsWith("Packages/"))
                    {
                        Object o = AssetDatabase.LoadMainAssetAtPath(p);
                        if (o == null) throw new ArgumentException("No asset at '" + p + "'.");
                        objs.Add(o);
                    }
                    else objs.Add(FindSceneObject(p));
                }
                Selection.objects = objs.ToArray();
                if (objs.Any(o => o is GameObject && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o))) && SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
                else if (objs.Count > 0) EditorGUIUtility.PingObject(objs[0]);
                return "Selected " + objs.Count + (objs.Count == 1 ? " object." : " objects.");
            }

            // ---------------------------------------------------------------- any asset
            private static Object LoadAssetForEdit(JSONNode a)
            {
                string path = Require(a, "path");
                string type = Str(a, "type");
                Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                if (all == null || all.Length == 0) throw new ArgumentException("No asset at '" + path + "'. Use list_assets to find it.");
                Object main = AssetDatabase.LoadMainAssetAtPath(path);
                if (string.IsNullOrEmpty(type)) return main;
                Object typed = all.FirstOrDefault(o => o != null && (TypeMatches(o.GetType(), type) || TypeKey(o.GetType().Name) == TypeKey(type)));
                if (typed == null) throw new ArgumentException("'" + path + "' has no " + type + ". It holds: " + string.Join(", ", all.Where(o => o != null).Select(o => o.GetType().Name).Distinct()));
                return typed;
            }
            private static string GetAssetProperties(JSONNode a)
            {
                Object obj = LoadAssetForEdit(a);
                if (obj is GameObject) throw new ArgumentException("That's a prefab. Place it in the scene (or open it) and use get_components / get_component_properties on it.");
                string filter = Str(a, "filter");
                SerializedObject so = new SerializedObject(obj);
                JSONObject o = new JSONObject();
                o["assetType"] = obj.GetType().Name;
                SerializedProperty it = so.GetIterator();
                bool enter = true;
                int count = 0;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.name == "m_Script") continue;
                    if (!string.IsNullOrEmpty(filter) && it.propertyPath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 && it.displayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    o[it.propertyPath] = PropToJson(it, 4);
                    if (++count >= 300) { o["…"] = "more properties omitted; use filter"; break; }
                }
                return o.ToString(2);
            }
            private static string SetAssetProperty(JSONNode a)
            {
                Object obj = LoadAssetForEdit(a);
                if (obj is GameObject) throw new ArgumentException("That's a prefab. Place it in the scene and change the object there, or use set_component_property on the placed copy.");
                string path = Require(a, "property_path");
                if (!a.HasKey("value")) throw new ArgumentException("Missing argument 'value'.");
                SerializedObject so = new SerializedObject(obj);
                SerializedProperty p = FindPropertyLoose(so, path);
                if (p == null) throw new ArgumentException("No property '" + path + "' on " + obj.GetType().Name + ". Use get_asset_properties to list them.");
                SetPropFromJson(p, a["value"]);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(obj);
                AssetDatabase.SaveAssetIfDirty(obj);
                so.Update();
                SerializedProperty check = so.FindProperty(p.propertyPath);
                return "Set " + obj.GetType().Name + "." + p.propertyPath + " in '" + AssetDatabase.GetAssetPath(obj) + "' to " + (check != null ? PropToJson(check, 2).ToString() : "(new value)");
            }
            private static string CreateAsset(JSONNode a)
            {
                string path = Require(a, "path");
                string typeName = Require(a, "type");
                if (!path.StartsWith("Assets/")) throw new ArgumentException("New assets go under Assets/.");
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new ArgumentException("'" + path + "' already exists.");
                string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(folder)) { System.IO.Directory.CreateDirectory(System.IO.Path.Combine(ProjectPath, folder)); AssetDatabase.Refresh(); }
                Object created;
                if (TypeKey(typeName) == "animatorcontroller") created = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
                else if (TypeKey(typeName) == "animationclip") { created = new AnimationClip(); AssetDatabase.CreateAsset(created, path); }
                else
                {
                    Type t = FindType(typeName, typeof(ScriptableObject)) ?? TypeCache.GetTypesDerivedFrom<ScriptableObject>().FirstOrDefault(x => !x.IsAbstract && TypeKey(x.Name) == TypeKey(typeName));
                    if (t == null) throw new ArgumentException("No asset type named '" + typeName + "'.");
                    created = ScriptableObject.CreateInstance(t);
                    AssetDatabase.CreateAsset(created, path);
                }
                AssetDatabase.SaveAssets();
                return "Created " + created.GetType().Name + " at '" + path + "'.";
            }
        }
    }

    internal static class AIStringExtensions
    {
        public static bool StartsWithSafe(this string s, string prefix) => s != null && s.StartsWith(prefix, StringComparison.Ordinal);
    }
}
