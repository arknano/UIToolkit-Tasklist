using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


// Derives from BaseField<string> base class. Represents a container for its input part.
    public class TaskListDropdown : BaseField<string>
    {
        //This is boilerplate
        public new class UxmlFactory : UxmlFactory<TaskListDropdown, UxmlTraits>
        {
        }

        //Also boilerplate-y, but here we can define fields that will show up in the UI Builder when we add this element
        public new class UxmlTraits : BaseFieldTraits<string, UxmlStringAttributeDescription>
        {
            //Unity uses hungarian scope format because I dunno they think its cool
            //Its important that the name parameter matches the class variable we define later
            UxmlStringAttributeDescription m_coolString =
                new UxmlStringAttributeDescription { name = "coolString", defaultValue = "" };

            //More boilerplate, but this part actually pulls the value of the field in the UI builder into the class
            //so we can use it as normal.
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ate = ve as TaskListDropdown;
                ate.coolString = m_coolString.GetValueFromBag(bag, cc);
            }
        }

        //This string isnt actually used at all, it's just here to show you how you can expose variables to the UI builder
        public string coolString { get; set; }
        
        //Despite the name of this class, its actually an element that contains a dropdown, not a dropdown itself
        //so we need a ref to the dropdown we'll spawn when it's constructed
        DropdownField dropdown;
        
        //Stores the loaded assets in a dropdown-friendly collection
        private Dictionary<string, TaskListSO> assetsDict = new Dictionary<string, TaskListSO>();

        // Custom controls need a default constructor. This default constructor calls the other constructor in this
        // class.
        public TaskListDropdown() : this(null)
        {
        }

        // This constructor allows users to set the contents of the label.
        public TaskListDropdown(string label) : base(label, null)
        {
            // Style the control overall.
            AddToClassList(ussClassName);
            
            //Spawn the dropdown and add it to this element
            dropdown = new DropdownField();
            Add(dropdown);
            
            dropdown.RegisterValueChangedCallback(SetValue);
        }


        private void SetValue(ChangeEvent<string> evt)
        {
            value = evt.newValue;
        }


        public DropdownField Setup(string startValue)
        {
            //Empty the choices, since we're gonna rebuild it
            dropdown.choices = null;
            
            //Get all the matching assets in the project and add them to the dictionary.
            var assets = GetAssetsOfType<TaskListSO>("Assets/");
            foreach (var asset in assets)
            {
                assetsDict.Add(asset.name, asset);
            }

            //We use the name of the asset as the key to the dicts and the choices, to make it clean to read in-UI
            //and also just easy to convert between the string choice and the asset itself.
            dropdown.choices = assetsDict.Keys.ToList();

            //Set the initial choice
            dropdown.SetValueWithoutNotify(string.IsNullOrEmpty(startValue) ? dropdown.choices[0] : startValue);

            return dropdown;
        }

        public Object GetTaskListAssetFromString(string key)
        {
            return (Object)assetsDict[key];
        }
        
        // Fetches all assets of a type in a given project location, recursively.
        // This is just a handy function ripped from Orca.
        private List<T> GetAssetsOfType<T>(string pathToAssets) where T : UnityEngine.Object
        {
            var guidList = AssetDatabase.FindAssets($"t:{typeof(T)}",
                new[] { pathToAssets });

            var results = new List<T>();
            if (guidList.Length == 0)
            {
                Debug.Log($"No assets of type {typeof(T)} found at {pathToAssets}.");
                return results;
            }

            foreach (var guid in guidList)
            {
                results.Add(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            return results;
        }
    }
