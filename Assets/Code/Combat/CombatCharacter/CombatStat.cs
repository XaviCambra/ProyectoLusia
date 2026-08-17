using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class CombatStat
{
    public float m_BaseValue;
    private List<float> m_StatModifiers = new();

    public float m_Value => m_BaseValue + m_StatModifiers.Sum();

    public void AddModifier(float _Modification) => m_StatModifiers.Add(_Modification);
    public void RemoveModifier(float _Modification) => m_StatModifiers.Remove(_Modification);
}
