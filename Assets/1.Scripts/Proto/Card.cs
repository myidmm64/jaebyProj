using UnityEngine;

public abstract class Card : MonoBehaviour
{
    protected int num = 0;

    public void Init(int num)
    {
        this.num = num;
        ChildInit(num);
    }
    protected abstract void ChildInit(int num);

}
