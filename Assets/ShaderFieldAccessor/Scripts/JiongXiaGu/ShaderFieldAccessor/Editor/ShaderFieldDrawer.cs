using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace JiongXiaGu.ShaderTools
{

    public interface IShaderFieldDrawer
    {
        string Name { get; }
        void CreateExpandItem(SignalPersistentSave signal);
        void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties, IList<string> keywords, SignalPersistentSave signal, int mask);
        int Collect(IReadOnlyList<string> keywords);
    }

    public class ShaderFieldDrawer : IShaderFieldDrawer
    {
        public string Name { get; set; }
        public IShaderFieldGroup Group { get; }
        public List<IShaderFieldDrawer> Drawers { get; }
        public int ExpandedID { get; private set; }

        public ShaderFieldDrawer(IShaderFieldGroup group, string displayName)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            Group = group;
            Name = displayName;
            Drawers = new List<IShaderFieldDrawer>(group.Children.Count);
            FindDrawers(Drawers, group);
        }

        public void FindDrawers(List<IShaderFieldDrawer> drawers, IShaderFieldGroup group)
        {
            foreach (var child in group.Children)
            {
                if (child is ShaderField)
                {
                    var field = (ShaderField)child;
                    drawers.Add(new FieldDrawer(field));
                }
                else if (child is ShaderKeyword)
                {
                    var field = (ShaderKeyword)child;
                    drawers.Add(new KeywordDrawer(field));
                }
                else if (child is ShaderEnumKeyword)
                {
                    var field = (ShaderEnumKeyword)child;
                    drawers.Add(new EnumKeywordDrawer(field));
                }
                else if (child is ShaderFieldGroup)
                {
                    var field = (ShaderFieldGroup)child;
                    var drawer = new GroupDrawer(field);
                    drawers.Add(drawer);
                    FindDrawers(drawer.Drawers, field);
                }
            }
        }

        public void CreateExpandItem(SignalPersistentSave signal)
        {
            ExpandedID = signal.CreateItem(Name);
            foreach (var drawer in Drawers)
            {
                drawer.CreateExpandItem(signal);
            }
        }

        public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties, IList<string> keywords, SignalPersistentSave signal, int mask)
        {
            bool isExpanded = signal[ExpandedID] = EditorGUILayout.Foldout(signal[ExpandedID], Name);
            if (isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var drawer in Drawers)
                    {
                        drawer.OnGUI(materialEditor, properties, keywords, signal, mask);
                    }
                }
            }
        }

        public int Collect(IReadOnlyList<string> keywords)
        {
            int mask = 0;
            foreach (var drawer in Drawers)
            {
                mask |= drawer.Collect(keywords);
            }
            return mask;
        }

        public bool ChangeDrawer(Func<IShaderFieldDrawer, IShaderFieldDrawer> getDrawer, params string[] path)
        {
            if (getDrawer == null)
                throw new ArgumentNullException(nameof(getDrawer));

            List<IShaderFieldDrawer> list = Drawers;

            for (int i = 0; i < path.Length; i++)
            {
                var name = path[i];
                var index = list.FindIndex(item => item.Name == name);
                if (index >= 0)
                {
                    var current = list[index];
                    if (i + 1 >= path.Length)
                    {
                        list[index] = getDrawer(current);
                        return true;
                    }

                    if (current is GroupDrawer)
                    {
                        list = ((GroupDrawer)current).Drawers;
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        public class GroupDrawer : IShaderFieldDrawer
        {
            public ShaderFieldGroup Group { get; private set; }
            public List<IShaderFieldDrawer> Drawers { get; private set; }
            public int ExpandedID { get; private set; }
            public string Name => Group.ReflectiveField.Name;

            public GroupDrawer(ShaderFieldGroup group)
            {
                Group = group;
                Drawers = new List<IShaderFieldDrawer>(group.Children.Count);
            }

            public int Collect(IReadOnlyList<string> keywords)
            {
                int mask = 0;
                foreach (var drawer in Drawers)
                {
                    mask |= drawer.Collect(keywords);
                }
                return mask;
            }

            public void CreateExpandItem(SignalPersistentSave signal)
            {
                ExpandedID = signal.CreateItem(Group.ReflectiveField.Name);
                foreach (var drawer in Drawers)
                {
                    drawer.CreateExpandItem(signal);
                }
            }

            public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties, IList<string> keywords, SignalPersistentSave signal, int mask)
            {
                if ((mask & Group.Mask) == 0)
                    return;

                bool isExpanded = signal[ExpandedID] = EditorGUILayout.Foldout(signal[ExpandedID], Group.ReflectiveField.Name);
                if (isExpanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        foreach (var drawer in Drawers)
                        {
                            drawer.OnGUI(materialEditor, properties, keywords, signal, mask);
                        }
                    }
                }
            }
        }

        public class FieldDrawer : IShaderFieldDrawer
        {
            public ShaderField Field { get; private set; }
            public string Name => Field.ReflectiveField.Name;

            public FieldDrawer(ShaderField field)
            {
                Field = field;
            }

            public int Collect(IReadOnlyList<string> keywords)
            {
                return 0;
            }

            public void CreateExpandItem(SignalPersistentSave signal)
            {
                return;
            }

            public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties, IList<string> keywords, SignalPersistentSave signal, int mask)
            {
                if ((mask & Field.Mask) == 0)
                    return;

                var materialProperty = ShaderDrawerHelper.PublicFindProperty(Field.ShaderFieldName, properties);
                string displayName = Field.ReflectiveField.Name;
                materialEditor.ShaderProperty(materialProperty, displayName);
            }
        }

        public class KeywordDrawer : IShaderFieldDrawer
        {
            public ShaderKeyword ShaderKeyword { get; private set; }
            public string Name => ShaderKeyword.ReflectiveField.Name;

            public KeywordDrawer(ShaderKeyword shaderKeyword)
            {
                ShaderKeyword = shaderKeyword;
            }

            public int Collect(IReadOnlyList<string> keywords)
            {
                return keywords.Contains(ShaderKeyword.Keyword) ? ShaderKeyword.Mask : 0;
            }

            public void CreateExpandItem(SignalPersistentSave signal)
            {
                return;
            }

            public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties, IList<string> keywords, SignalPersistentSave signal, int mask)
            {
                var targetMat = materialEditor.target as Material;
                bool isEnabled = targetMat.IsKeywordEnabled(ShaderKeyword.Keyword);

                bool newValue = EditorGUILayout.Toggle(ShaderKeyword.ReflectiveField.Name, isEnabled);
                if (newValue)
                {
                    keywords.Add(ShaderKeyword.Keyword);
                }
            }
        }

        public class EnumKeywordDrawer : IShaderFieldDrawer
        {
            public ShaderEnumKeyword EnumKeyword { get; }
            public string Name => EnumKeyword.ReflectiveField.Name;

            public EnumKeywordDrawer(ShaderEnumKeyword keyword)
            {
                EnumKeyword = keyword;
            }

            public int Collect(IReadOnlyList<string> keywords)
            {
                foreach (var item in EnumKeyword)
                {
                    var keyword = item.Value;
                    if (keywords.Contains(keyword))
                    {
                        return item.Key;
                    }
                }
                return 0;
            }

            public void CreateExpandItem(SignalPersistentSave signal)
            {
                return;
            }

            public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties, IList<string> keywords, SignalPersistentSave signal, int mask)
            {
                int newSelecteIndex;
                string displayName = EnumKeyword.ReflectiveField.Name;
                var targetMat = materialEditor.target as Material;

                int selecteIndex = 0;
                string keyword = null;

                foreach (var item in EnumKeyword)
                {
                    keyword = item.Value;
                    if (keyword != null && targetMat.IsKeywordEnabled(keyword))
                    {
                        break;
                    }
                    selecteIndex++;
                }

                if (selecteIndex >= EnumKeyword.EnumNames.Length)
                {
                    selecteIndex = EnumKeyword.EmptyKeywordIndex;
                }

                newSelecteIndex = EditorGUILayout.Popup(displayName, selecteIndex, EnumKeyword.EnumNames);
                keyword = EnumKeyword.GetKeyword(newSelecteIndex * 2);
                if (keyword != null)
                    keywords.Add(keyword);
            }
        }
    }
}
