using UnityEngine;



namespace HeadChopping
{


    public static class MathExtensions
    {
        public static Vector3 WithY(this Vector3 vector, float newY)
        {
            Vector3 result = vector;
            result.y = newY;
            return result;
        }
    }



}