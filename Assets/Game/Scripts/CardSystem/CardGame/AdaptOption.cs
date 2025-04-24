using System;

public class AdaptOption
{
    public string name;
    public Action applyEffect;

    public AdaptOption(string name, Action applyEffect)
    {
        this.name = name;
        this.applyEffect = applyEffect;
    }
}