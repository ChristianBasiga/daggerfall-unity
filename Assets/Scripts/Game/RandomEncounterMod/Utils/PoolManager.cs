using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DaggerfallRandomEncountersMod.Utils
{
    /// <summary>
    /// Pool manager is a singleton in charge of managing multiple pools
    /// for randomencounters, random encounter resources, etc.
    /// Doesn't have to be MonoBehaviour, would change just for organization purposes
    /// and so already thread safe too.
    /// </summary>
    /// 
    public class PoolManager 
    {

        private static PoolManager instance;
        private GameObject poolHolder;

        
        private Queue<Reusable> pool;
        private int poolCapacity;

        public static PoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PoolManager();
                    instance.poolHolder = new GameObject("EncounterPool");
                }
                return instance;
            }
        }


        public int PoolCapacity
        {
            get
            {
                return poolCapacity;
            }
            set
            {
                poolCapacity = value;

                pool = new Queue<Reusable>(poolCapacity);

                for (int i = 0; i < poolCapacity; ++i)
                {
                    GameObject holder = new GameObject("Reusable");
                    holder.transform.parent = poolHolder.transform;
                    holder.SetActive(false);
                    Reusable reusable = holder.AddComponent<Reusable>();
                    //Assigning callback to insert itself back into pool.
                    reusable.OnDoneUsing += (Reusable doneUsing) =>
                    {
                        //If already at capactiy don't put back in pool, just deallocate it.
                        if (pool.Count == PoolCapacity)
                        {
                            MonoBehaviour.Destroy(doneUsing.gameObject);
                        }
                        else
                        {
                            doneUsing.gameObject.SetActive(false);

                            pool.Enqueue(doneUsing);
                        }
                    };

                    pool.Enqueue(reusable);
                }


            }
        }

        public Reusable acquireObject()
        {
            if (pool == null || pool.Count == 0) {

                 GameObject holder = new GameObject();
                //This is actually bad, since I don't want to be reusable cause then capacity is wrong.
                return holder.AddComponent<Reusable>();
             }


            //I usually have manager assign the callbacks to put back into queueu
            //I could have encounterManager do that with encounters too
            //just add onto the OnEnd event. That's probably best,
            //the only problem with it is I'm now passing responsibility of putting back into pool in hands of client.
            //so no longer really managed by this.

            Reusable reusable = pool.Dequeue();
            reusable.gameObject.SetActive(true);
            
            return reusable;
        }



        
    }
}