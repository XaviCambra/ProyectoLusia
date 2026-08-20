using System.Collections;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    float m_CurrentHealth;
    float m_MaxHealth;

    [SerializeField] bool m_HasHealthBar;
    [SerializeField] GameObject m_HealthBar;

    private void Start()
    {
        m_HasHealthBar = m_HealthBar != null;
    }

    public void SetNewHealth(float _Vitality)
    {
        m_CurrentHealth = m_MaxHealth = _Vitality*10;
    }

    public void TakeDamage(float _Amount)
    {
        if(m_HasHealthBar)
        {

            return;
        }
        
        m_CurrentHealth = Mathf.Max(0, m_CurrentHealth - _Amount);

        if (m_CurrentHealth <= 0)
            OnDead();
    }

    public void Heal(float _Value)
    {
        if (m_HasHealthBar)
        {

            return;
        }

        m_CurrentHealth = Mathf.Min(m_MaxHealth, m_CurrentHealth + _Value);
        if (m_CurrentHealth <= 0)
            OnDead();
    }

    public void OnDead()
    {

    }

    IEnumerator HealthBarAnimation()
    {

        yield return null;
    }
}
