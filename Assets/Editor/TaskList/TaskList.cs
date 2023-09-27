using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class TaskList : EditorWindow
{
    //Define the locations of assets as consts so its easy to change if need be
    private const string taskListLayoutPath = "Assets/Editor/TaskList/CoolTaskList.uxml";
    private const string taskEntryLayoutPath = "Assets/Editor/TaskList/TaskEntry.uxml";
    private const string taskListPath = "Assets/Editor/TaskList/Example Task List.asset";
    
    //We will do a lot of querying by ID name, so its best to define the strings as variables for easier refactoring
    private const string taskListAssetPickerID = "TaskListPicker";
    private const string taskListViewID = "TaskList";
    private const string newTaskTextFieldID = "NewTaskField";
    private const string newTaskButtonID = "NewTaskButton";
    private const string taskTextID = "TaskText";
    private const string deleteButtonID = "DeleteButton";
    private const string completeToggleID = "CompleteToggle";
    private const string taskListDropdownID = "TaskListDropdown";

    //References to the instantiated UI elements
    private ObjectField assetPicker;
    private ListView taskList;
    private Button addTaskButton;
    private TextField newTaskField;
    private TaskListDropdown taskListDropdown;
    
    //The document root
    private VisualElement root;

    //A reference to the task entry ui document, loaded and ready for instantiation
    private VisualTreeAsset taskEntryTemplate;

    private TaskListSO tasksSO;
    
    [MenuItem("Tools/Task List")]
    public static void ShowWindow()
    {
        TaskList window = GetWindow<TaskList>();
        window.titleContent = new GUIContent("Cool Task List");
    }

    private void CreateGUI()
    {
        root = rootVisualElement;
        
        //Fetch the main layout and instantiate it into our editor window
        VisualTreeAsset original =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(taskListLayoutPath);
        root.Add(original.Instantiate());
        
        //Load the task entry UI document so we can instantiate it later
        taskEntryTemplate =  AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(taskEntryLayoutPath);

        //Store references to the things we'll actually be working with
        assetPicker = root.Q<ObjectField>(taskListAssetPickerID);
        taskList = root.Q<ListView>(taskListViewID);
        addTaskButton = root.Q<Button>(newTaskButtonID);
        newTaskField = root.Q<TextField>(newTaskTextFieldID);
        taskListDropdown = root.Q<TaskListDropdown>(taskListDropdownID);

        //Any time the asset picker is changed, we'll immediately load the new tasks.
        //Since changes to tasks are saved immediately, there's no risk of data loss
        assetPicker.RegisterValueChangedCallback(evt =>
        {
            LoadTasks();
        });
        
        //Set up the task list dropdown (add the choices to the dropdown)
        //We also register a callback so the asset picker field matches (the Setup function returns the dropdown!)
        taskListDropdown.Setup(assetPicker.value ? assetPicker.value.name : "").RegisterValueChangedCallback(evt =>
        {
            //Note that we dont SetValueWithoutNotify here, so the tasklist update functions run.
            assetPicker.value = taskListDropdown.GetTaskListAssetFromString(evt.newValue);
        });

        
        //Subscribe to the add task button clicked event
        addTaskButton.clicked += AddTask;
        
        //Set the functions that will handle creating and modifying elements of the task list
        //This is more complex than using a ScrollView (where we can just instantiate the list items and call it a day)
        //But using a ListView means we get reordering for free (and its highly performant for huge lists)
        taskList.makeItem = MakeTaskEntryItem;
        taskList.bindItem = BindTaskEntryItem;
    }
    
    //When a new list item is needed, this function is called, which just spawns the ui element
    private VisualElement MakeTaskEntryItem() {return taskEntryTemplate.Instantiate();}

    //After the list item is created, this function fills out the element with the relevant data
    private void BindTaskEntryItem(VisualElement e, int index)
    {
        var task = tasksSO.tasks[index];
        
        //Set the entry state based on the data in the SO
        e.Q<Label>(taskTextID).text = task.text;
        e.Q<Toggle>(completeToggleID).SetValueWithoutNotify(task.complete);
        
        //If the task is complete, add the style class to make it greyed out
        if (task.complete) e.AddToClassList("taskEntryComplete");

        //This callback will update the data in the SO as soon as the state changes!
        e.Q<Toggle>(completeToggleID).RegisterValueChangedCallback(evt =>
        {
            task.complete = evt.newValue;
            LoadTasks();
        });

        e.Q<Button>(deleteButtonID).clicked += () =>
        {
            tasksSO.tasks.Remove(task);
            LoadTasks();
        };
    }

    //Adds a new task to the task list in the SO, then loads again so it appears in the ui
    private void AddTask()
    {
        tasksSO.tasks.Add(new TaskListSO.Task(newTaskField.value, false));
        newTaskField.SetValueWithoutNotify("");
        LoadTasks();
    }
    
    private void LoadTasks()
    {
        //Fetch the scriptable object from the picker field
        tasksSO = assetPicker.value as TaskListSO;
        
        //Bind the task ListView to the task list in the SO. Notably this will mean if we reorder tasks
        //in the tool, they will automatically reorder in the SO list too!
        taskList.itemsSource = tasksSO.tasks;
        
        //Refresh the task list with the new data
        taskList.Rebuild();
        
        //Since load tasks gets called any time the UI changes anything, we will auto-save those changes
        //Plus, the entire project. Possibly a bit heavy handed but broadly pretty useful!
        EditorUtility.SetDirty(tasksSO);
        AssetDatabase.SaveAssets();
    }
}
