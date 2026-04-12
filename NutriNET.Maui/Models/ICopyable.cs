namespace NutriNET.Maui.Models
{
    interface ICopyable <T>
    {
        void CopyFrom(T original);
    }
}
