using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro global de contactos del chat. Referenciado por ChatPhoneApp
/// y por cualquier sistema externo que necesite desbloquear conversaciones.
/// </summary>
[CreateAssetMenu(fileName = "ChatRegistry", menuName = "Chat/Chat Registry")]
public class ChatRegistry : ScriptableObject
{
    public List<ChatContact> contacts = new();
}
