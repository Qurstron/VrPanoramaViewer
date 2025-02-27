using UnityEngine;

public class SelectableContainer<T> : MonoBehaviour
{
    public T selectable;
}

public class WorldSelectableContainer : SelectableContainer<WorldSelectable>
{

}

public class ObjectSelectableContainer : SelectableContainer<ObjectSelectable>
{

}

public class SceneRootContainer : SelectableContainer<SceneRoot>
{

}