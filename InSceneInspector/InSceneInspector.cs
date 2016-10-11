using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InSceneInspector
{
    public class InSceneInspector : MonoBehaviour
    {
        public GameObject stringPrefab;
        public GameObject boolPrefab;
        public bool inspectFields;
        public bool inspectProperties;

        public DropDownList dropDownListPrefab;
        public Button dropDownButtonPrefab;
        public GameObject fieldPanel;

        public DropDownMenu objectPicker;
        public DropDownMenu componentPicker;

        public GameObject defaultObject;
        public string defaultComponent;
        public int defaultComponentIndex;

        private GameObject gameObjectToInspect;
        private string componentToInspect;

        private bool open = false;
        private bool toggling = false;
        private Vector3 goalTogglePosition;
        private float maxToggleDelta = 20;

        private List<string> ignoredMembers = new List<string> {
            "useGuiLayout",
            "enabled",
            "tag",
            "name",
            "hideFlags"
        };

        private List<GameObject> fieldsAdded = new List<GameObject> { };

        GameObject[] gameObjects;

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            //If mouse is not over the panel inspect. (This allows for values to update when they are changed elsewhere)
            //Inspect();

            UpdateToggl();
        }

        public void Initialize()
        {
            BuildGameObjectDropDownMenu();

            if (defaultObject != null)
            {
                gameObjectToInspect = defaultObject;
                objectPicker.triggerButton.GetComponentInChildren<Text>().text = GetInspectorStyleName(gameObjectToInspect.name);
                BuildComponentDropDownMenu();

                if (defaultComponent != null)
                {
                    componentToInspect = defaultComponent;
                    componentPicker.triggerButton.GetComponentInChildren<Text>().text = GetInspectorStyleName(componentToInspect);

                    Inspect();
                }
                else
                {
                    componentPicker.triggerButton.GetComponentInChildren<Text>().text = "Select Component";
                }
            }
            else
            {
                objectPicker.triggerButton.GetComponentInChildren<Text>().text = "Select Object";
            }

        }

        public void Toggle()
        {
            if (!toggling)
            {
                float xChange = GetComponent<RectTransform>().sizeDelta.x - 22;
                Vector3 postionChange = Vector3.zero;

                if (open)
                {
                    postionChange = new Vector3(-xChange, 0, 0);
                    open = false;
                }
                else
                {
                    postionChange = new Vector3(xChange, 0, 0);
                    open = true;
                }

                goalTogglePosition = transform.position + postionChange;

                toggling = true;
            }
        }

        public void UpdateToggl()
        {
            if (toggling)
            {
                transform.position = Vector3.MoveTowards(transform.position, goalTogglePosition, maxToggleDelta);

                if (Vector3.Distance(transform.position, goalTogglePosition) < maxToggleDelta)
                {
                    toggling = false;
                    transform.position = goalTogglePosition;
                }
            }
        }

        public void Reset()
        {
            clearFields();
            objectPicker.ClearContent();
            componentPicker.ClearContent();
        }

        public void BuildGameObjectDropDownMenu()
        {
            objectPicker.ClearContent();

            objectPicker.triggerButton.onClick.AddListener(delegate ()
            {
                componentPicker.HideMenu();
            });

            gameObjects = GameObject.FindObjectsOfType<GameObject>();
            List<GameObject> rootGameObjects = new List<GameObject> { };

            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject.transform.root.gameObject.GetInstanceID() == gameObject.GetInstanceID())
                {
                    rootGameObjects.Add(gameObject);

                    DropDownList dropDownList = Instantiate<DropDownList>(dropDownListPrefab);
                    dropDownList.text.text = GetInspectorStyleName(gameObject.name);
                    objectPicker.AddDropDownList(dropDownList);

                    BuildDropDownList(dropDownList, gameObject);
                }
            }

            /*foreach (GameObject gameObject in gameObjects)
            {
                string gameObjectName = gameObject.name;
                int gameObjectId = gameObject.GetInstanceID();
                Button gameObjectButton = Instantiate<Button>(dropDownButtonPrefab);
                Text buttonText = gameObjectButton.GetComponentInChildren<Text>();
                buttonText.text = GetInspectorStyleName(gameObjectName);

                objectPicker.AddButton(gameObjectButton);

                gameObjectButton.onClick.AddListener(delegate ()
                {
                    foreach (GameObject tmpGameObject in gameObjects)
                    {
                        if (tmpGameObject.GetInstanceID() == gameObjectId)
                        {
                            gameObjectToInspect = tmpGameObject;
                            //Debug.Log(gameObjectToInspect.name);
                        }
                    }

                    Text triggerButtonText = objectPicker.triggerButton.GetComponentInChildren<Text>();
                    triggerButtonText.text = GetInspectorStyleName(gameObjectName);

                    BuildComponentDropDownMenu();
                });
            }*/
        }

        public void BuildDropDownList(DropDownList parentList, GameObject parent)
        {
            parentList.carrot.onValueChanged.AddListener(delegate (bool value)
            {
                parentList.children.SetActive(value);
            });

            parentList.button.onClick.AddListener(delegate ()
            {
                objectPicker.HideMenu();

                foreach (GameObject tmpGameObject in gameObjects)
                {
                    if (tmpGameObject.GetInstanceID() == parent.GetInstanceID())
                    {
                        gameObjectToInspect = tmpGameObject;
                        clearFields();
                        Text ComponentButtonText = componentPicker.triggerButton.GetComponentInChildren<Text>();
                        ComponentButtonText.text = "Select Component";
                    }
                }

                Text triggerButtonText = objectPicker.triggerButton.GetComponentInChildren<Text>();
                triggerButtonText.text = GetInspectorStyleName(GetInspectorStyleName(parent.name));

                BuildComponentDropDownMenu();
            });

            if (parent.transform.childCount == 0)
            {
                parentList.carrot.GetComponent<CanvasGroup>().alpha = 0;
            }
            else
            {
                for (int i = 0; i < parent.transform.childCount; i++)
                {
                    GameObject child = parent.transform.GetChild(i).gameObject;

                    if (child.GetComponent<InSceneInspector>() == null)
                    {
                        DropDownList childList = Instantiate<DropDownList>(dropDownListPrefab);
                        childList.text.text = GetInspectorStyleName(child.name);
                        if (!child.active)
                        {
                            Color newColor = childList.text.color;
                            newColor.a = .5f;
                            childList.text.color = newColor;
                        }
                        childList.transform.SetParent(parentList.children.transform, false);

                        BuildDropDownList(childList, child);
                    }
                }
            }
        }

        public void BuildComponentDropDownMenu()
        {
            componentPicker.ClearContent();
            componentPicker.triggerButton.onClick.AddListener(delegate ()
            {
                objectPicker.HideMenu();
            });

            List<Component> components = new List<Component> { };
            gameObjectToInspect.GetComponents<Component>(components);

            foreach (Component component in components)
            {
                string compoentName = component.GetType().Name;
                Button componentButton = Instantiate<Button>(dropDownButtonPrefab);
                Text buttonText = componentButton.GetComponentInChildren<Text>();
                buttonText.text = GetInspectorStyleName(compoentName);

                componentPicker.AddButton(componentButton);

                componentButton.onClick.AddListener(delegate ()
                {
                    componentToInspect = compoentName;

                    Text triggerButtonText = componentPicker.triggerButton.GetComponentInChildren<Text>();
                    triggerButtonText.text = GetInspectorStyleName(compoentName);

                    Inspect();
                });
            }
        }

        public void Inspect()
        {
            clearFields();

            if (inspectFields)
            {
                InspectFields();
            }

            if (inspectProperties)
            {
                InspectProperties();
            }
        }

        private void clearFields()
        {
            foreach (GameObject field in fieldsAdded)
            {
                DestroyImmediate(field);
            }

            fieldsAdded.Clear();
        }

        public static string GetInspectorStyleName(string str)
        {
            return Regex.Replace(
                Regex.Replace(
                    str.Replace(" ", ""),
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }

        private void InspectProperties()
        {
            List<Component> components = new List<Component> { };
            gameObjectToInspect.GetComponents<Component>(components);

            foreach (Component component in components)
            {
                if (component.GetType().Name.Equals(componentToInspect))
                {
                    PropertyInfo[] properties = component.GetType().GetProperties();

                    foreach (PropertyInfo property in properties)
                    {
                        if (property.CanWrite && !ignoredMembers.Contains(property.Name))
                        {
                            GameObject clone;

                            if (property.PropertyType.Equals(typeof(Boolean)))
                            {
                                clone = (GameObject)Instantiate(boolPrefab, Vector3.zero, Quaternion.identity);

                                bool value = (bool)property.GetValue(component, null);
                                Transform toggleTransform = clone.transform.Find("Toggle");
                                Toggle toggle = toggleTransform.GetComponent<Toggle>();
                                toggle.isOn = value;

                                int componentId = component.GetInstanceID();
                                Type componentType = component.GetType();
                                string propertyName = property.Name;
                                toggle.onValueChanged.AddListener(delegate (bool call)
                                {
                                    SetPropertyValue(componentId, componentType, propertyName, call);
                                });
                            }
                            else
                            {
                                Debug.Log("Inspecting: " + property.Name);

                                if (property.Name == "parent")
                                {
                                    continue;
                                }

                                clone = (GameObject)Instantiate(stringPrefab, Vector3.zero, Quaternion.identity);

                                object value = property.GetValue(component, null);
                                Transform inputField = clone.transform.Find("Input Field");
                                Transform placeholder = inputField.Find("Placeholder");
                                Text text = placeholder.GetComponent<Text>();
                                text.text = value.ToString();
                                InputField inputFieldComponent = inputField.GetComponent<InputField>();

                                int componentId = component.GetInstanceID();
                                Type componentType = component.GetType();
                                string propertyName = property.Name;
                                Type propertyType = property.PropertyType;
                                inputFieldComponent.onEndEdit.AddListener(delegate (string str)
                                {
                                    object convertedValue = str;

                                    if (propertyType.Equals(typeof(Vector2)))
                                    {
                                        string[] parsedString = str.Split(new char[] { ' ', ',', ')', '(' }, StringSplitOptions.RemoveEmptyEntries);
                                        convertedValue = new Vector2(float.Parse(parsedString[0]), float.Parse(parsedString[1]));
                                    }
                                    else if (propertyType.Equals(typeof(Vector3)))
                                    {
                                        string[] parsedString = str.Split(new char[] { ' ', ',', ')', '(' }, StringSplitOptions.RemoveEmptyEntries);
                                        convertedValue = new Vector3(float.Parse(parsedString[0]), float.Parse(parsedString[1]), float.Parse(parsedString[2]));
                                    }

                                    SetPropertyValue(componentId, componentType, propertyName, convertedValue);
                                });
                            }

                            clone.transform.SetParent(fieldPanel.transform, false);
                            fieldsAdded.Add(clone);
                            clone.transform.Find("Field Name").GetComponent<Text>().text = GetInspectorStyleName(property.Name);
                        }
                    }
                }
            }
        }

        private void SetPropertyValue(int componentId, Type componentType, string propertyName, object value)
        {
            Component[] components = (Component[])Component.FindObjectsOfType(componentType);
            foreach (Component component in components)
            {
                if (component.GetInstanceID() == componentId)
                {
                    PropertyInfo property = component.GetType().GetProperty(propertyName);
                    Debug.Log("Setting " + component.ToString() + " property " + propertyName.ToString() + " to (" + property.PropertyType.ToString() + ") " + value.ToString());
                    property.SetValue(component, Convert.ChangeType(value, property.PropertyType), null);
                }
            }
        }

        private void InspectFields()
        {
            List<Component> components = new List<Component> { };
            gameObjectToInspect.GetComponents<Component>(components);

            foreach (Component component in components)
            {
                if (component.GetType().Name.Equals(componentToInspect))
                {
                    FieldInfo[] fields = component.GetType().GetFields(
                             BindingFlags.Public |
                             BindingFlags.NonPublic |
                             BindingFlags.Instance);

                    foreach (FieldInfo field in fields)
                    {
                        //if (field.IsPublic || !field.IsNotSerialized)
                        {
                            GameObject clone;

                            if (field.FieldType.Equals(typeof(Boolean)))
                            {
                                clone = (GameObject)Instantiate(boolPrefab, Vector3.zero, Quaternion.identity);

                                bool value = (bool)field.GetValue(component);
                                Transform toggleTransform = clone.transform.Find("Toggle");
                                Toggle toggle = toggleTransform.GetComponent<Toggle>();
                                toggle.isOn = value;

                                int componentId = component.GetInstanceID();
                                Type componentType = component.GetType();
                                string fieldName = field.Name;
                                toggle.onValueChanged.AddListener(delegate (bool call)
                                {
                                    SetFieldValue(componentId, componentType, fieldName, call);
                                });
                            }
                            else
                            {
                                clone = (GameObject)Instantiate(stringPrefab, Vector3.zero, Quaternion.identity);

                                object value = field.GetValue(component);
                                Transform inputField = clone.transform.Find("Input Field");
                                Transform placeholder = inputField.Find("Placeholder");
                                Text text = placeholder.GetComponent<Text>();
                                text.text = value.ToString();
                                InputField inputFieldComponent = inputField.GetComponent<InputField>();

                                int componentId = component.GetInstanceID();
                                Type componentType = component.GetType();
                                string fieldName = field.Name;
                                Type fieldType = field.FieldType;
                                inputFieldComponent.onEndEdit.AddListener(delegate (string str)
                                {
                                    object convertedValue = str;

                                    if (fieldType.Equals(typeof(Vector2)))
                                    {
                                        string[] parsedString = str.Split(new char[] { ' ', ',', ')', '(' }, StringSplitOptions.RemoveEmptyEntries);
                                        convertedValue = new Vector2(float.Parse(parsedString[0]), float.Parse(parsedString[1]));
                                    }
                                    else if (fieldType.Equals(typeof(Vector3)))
                                    {
                                        string[] parsedString = str.Split(new char[] { ' ', ',', ')', '(' }, StringSplitOptions.RemoveEmptyEntries);
                                        convertedValue = new Vector3(float.Parse(parsedString[0]), float.Parse(parsedString[1]), float.Parse(parsedString[2]));
                                    }

                                    SetFieldValue(componentId, componentType, fieldName, convertedValue);
                                });
                            }

                            clone.transform.SetParent(fieldPanel.transform, false);
                            fieldsAdded.Add(clone);
                            clone.transform.Find("Field Name").GetComponent<Text>().text = GetInspectorStyleName(field.Name);
                        }
                    }
                }
            }
        }

        private void SetFieldValue(int componentId, Type componentType, string fieldName, object value)
        {
            Component[] components = (Component[])Component.FindObjectsOfType(componentType);
            foreach (Component component in components)
            {
                if (component.GetInstanceID() == componentId)
                {
                    FieldInfo field = component.GetType().GetField(fieldName,
                             BindingFlags.Public |
                             BindingFlags.NonPublic |
                             BindingFlags.Instance);
                    Debug.Log("Setting " + component.ToString() + " field " + fieldName.ToString() + " to (" + field.FieldType.ToString() + ") " + value.ToString());
                    field.SetValue(component, Convert.ChangeType(value, field.FieldType));
                }
            }
        }
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(InSceneInspector))]
    [ExecuteInEditMode]
    public class InSceneInspectorEditor : Editor
    {
        public static bool useCustomInspector = true;

        //private int defaultComponentIndex = 0;
        private static List<string> componentChoices = null;

        public override void OnInspectorGUI()
        {
            if (componentChoices == null)
            {
                UpdateComponentChoices();
            }

            if (!useCustomInspector)
            {
                DrawDefaultInspector();
                EditorGUILayout.HelpBox("For more intuive editing check Use Custom Inspector", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Defualt Object");
                DefaultObject = (GameObject)EditorGUILayout.ObjectField(DefaultObject, typeof(GameObject), true);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Defualt Component");
                if (DefaultObject != null)
                {
                    DefaultComponentIndex = EditorGUILayout.Popup(DefaultComponentIndex, componentChoices.ToArray());
                }
                else
                {
                    GUI.enabled = false;
                    DefaultComponentIndex = EditorGUILayout.Popup(0, new string[] { "" });
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();

                GUIContent runButtonText = new GUIContent("Run");
                var runButtonRect = GUILayoutUtility.GetRect(runButtonText, GUI.skin.button, GUILayout.ExpandWidth(false));
                runButtonRect.width = 150;
                runButtonRect.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, runButtonRect.center.y);
                if (GUI.Button(runButtonRect, runButtonText, GUI.skin.button))
                {
                    InSceneIspectorInstance.Initialize();
                }

                GUIContent resetButtonText = new GUIContent("Reset");
                var resetButtonRect = GUILayoutUtility.GetRect(resetButtonText, GUI.skin.button, GUILayout.ExpandWidth(false));
                resetButtonRect.width = 150;
                resetButtonRect.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, resetButtonRect.center.y);
                if (GUI.Button(resetButtonRect, resetButtonText, GUI.skin.button))
                {
                    InSceneIspectorInstance.Reset();
                }

                EditorGUILayout.HelpBox("For full editing capabilities un-check Use Custom Inspector", MessageType.Info);
            
                EditorUtility.SetDirty(InSceneIspectorInstance);
            }

            useCustomInspector = EditorGUILayout.Toggle("Use Custom Inspector", useCustomInspector);
        }

        private InSceneInspector InSceneIspectorInstance
        {
            get { return (InSceneInspector)target; }
        }
     
        private void UpdateComponentChoices()
        {
            if (DefaultObject != null)
            {
                Component[] components = DefaultObject.GetComponents<Component>();

                componentChoices = new List<string>(components.Where(x => x != null).Select(x => x.GetType().Name).ToArray());
                componentChoices.Insert(0, "Select Component");
                DefaultComponentIndex = 0;
            }
        }

        private GameObject DefaultObject
        {
            get { return InSceneIspectorInstance.defaultObject; }
            set
            {
                if (InSceneIspectorInstance.defaultObject != value)
                {
                    InSceneIspectorInstance.defaultObject = value;
                    UpdateComponentChoices();
                }
            }
        }

        public int DefaultComponentIndex
        {
            get
            {
                return InSceneIspectorInstance.defaultComponentIndex;
            }

            set
            {
                InSceneIspectorInstance.defaultComponentIndex = value;
                InSceneIspectorInstance.defaultComponent = componentChoices[InSceneIspectorInstance.defaultComponentIndex];
            }
        }
    }

#endif
}