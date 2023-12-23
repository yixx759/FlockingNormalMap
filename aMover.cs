using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Random = UnityEngine.Random;


//pass stuff to ref

struct Boid
{
    public Vector3 pos;
    public UnityEngine.Quaternion head;

}

struct psedotrans
{
    public Vector3 pos;
    public Quaternion rot;

  

}

struct  locforbug
{
    public float2 pos;
    public float rot;
}





public class aMover : MonoBehaviour
{

    
    [SerializeField] private Transform target;

    private locforbug[] locs;
    public ComputeShader draw;
    public ComputeShader blanker;
    public ComputeBuffer drawer;
    [SerializeField] private float normstrength;
    private RenderTexture targettotex;
    private RenderTexture targettotex2;
    public Vector2 loc;
    [ Range(0,1) ,SerializeField] 
    private float size;
    [Range(0,1),SerializeField] private float ang;
    [SerializeField] private Vector2 size2;

    [SerializeField] private int maggotcount;
    //use the z value to acess the location 
    
    private psedotrans[] transers;
 
    private NativeArray<Boid> listnu;
    private NativeArray<psedotrans> psedotrans;
    private NativeArray<locforbug> locsnat;
    [SerializeField, Range(0,1)]
    private float effect;

   

    private Renderer reftouse;
    private int sizt1;
    
    
    private Vector3 startpos;
    // Start is called before the first frame update
    void Start()
    {
        sizt1 = Marshal.SizeOf<locforbug>();
        reftouse =  this.GetComponent<Renderer>() ;
       
        transers = new psedotrans[maggotcount];
      
    
        startpos = transform.position;
     
       
         listnu = new NativeArray<Boid>(transers.Length, Allocator.Persistent);
         psedotrans = new NativeArray<psedotrans>(transers.Length, Allocator.Persistent);
         locsnat = new NativeArray<locforbug>(transers.Length, Allocator.Persistent);
         for (int i = 0; i < transers.Length; i++)
         {
            
             transers[i].pos = new Vector3(Random.Range(0,512), 0, Random.Range(0,512));
             transers[i].rot = Quaternion.identity;
             psedotrans[i] = transers[i];
             Boid tmp = new Boid();
             tmp.pos = transers[i].pos;
             tmp.head = transers[i].rot;
             listnu[i] =  tmp;
            

         }
         
         targettotex = new RenderTexture(512, 512, 0, RenderTextureFormat.R16, RenderTextureReadWrite.Linear);
         targettotex2 = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
         targettotex.enableRandomWrite = true;
         targettotex2.enableRandomWrite = true;
         targettotex.Create();
         targettotex2.Create();
         draw.SetFloat("sizeofbuff", psedotrans.Length);
         locs = new locforbug[psedotrans.Length];
       
         drawer = new ComputeBuffer(psedotrans.Length, sizt1);
       reftouse.material.SetTexture("_HeightMap", targettotex2);
      
    }

    // Update is called once per frame
    void Update()
    {
        
    
        //this can be used to change behaviourn in this case if you move the plane the boieds will scatter/
        effect = Mathf.Lerp(1, 0, Mathf.Abs((transform.position.y - startpos.y)));
      
    //flocking simulation
   nuMoverJOB here = new nuMoverJOB(){time = Time.deltaTime, pos = listnu,target = target.position,effet = effect, player =psedotrans , locer = locsnat};

        JobHandle a = here.Schedule(psedotrans.Length,32 );
        a.Complete();

        docpmutstuff();






    }

    void docpmutstuff()
    {
        
       //to allow for stacking multiple threads of the flocking simulation
       //must be reset outside ocmpute shader.
       Graphics.Blit(Texture2D.blackTexture,  targettotex);
      

        //one compute to draw a height map simulating the flocking
        //second to transform this into a normal map
        //then this is fed to the material as a normal map
        drawer.SetData(locsnat);
     
        draw.SetTexture(0, "Result", targettotex);
        draw.SetVector("num", loc);
        draw.SetVector("size2", size2);
        draw.SetBuffer(0,"todraw", drawer );
        draw.Dispatch(0,(512/8),(512/8),psedotrans.Length);
        blanker.SetTexture(0, "Result", targettotex2);
        blanker.SetTexture(0, "read", targettotex);
        blanker.SetFloat("NormalStrength",normstrength);
        blanker.Dispatch(0,512/8, 512/8,1);
        
        
 
        
        
        
    }



    private void OnDestroy()
    {
        
        drawer.Dispose();
       
        listnu.Dispose();
        psedotrans.Dispose();
        locsnat.Dispose();


    }


    [BurstCompile]
     struct nuMoverJOB : IJobParallelFor
    {
        
      //refactor by only using vector 2
        
        public static float nfmod(float a,float b)
        {
            return a - b * Mathf.Floor(a / b);
        }
        public NativeArray<Boid> pos;
        public NativeArray<psedotrans> player;
        public NativeArray<locforbug> locer;
      
        [ReadOnly]   public Vector3 target;
      [ReadOnly] public float effet;
      [ReadOnly]  public float time;
      
     
        public void Execute(int blank)
        {

            
   

            Vector3 posupdate = Vector3.zero;
            Vector3 fowupdate = Vector3.zero;
            int count = 0;
            Vector3 post = player[blank].pos;
          Vector3 sep = Vector3.zero;
          Vector3 forw = (player[blank].rot * Vector3.forward);
          Vector3 right = (player[blank].rot * Vector3.right);
           
          
          //Goes through and collect the average heading direction and position
          //sending the player towards this will have them move in a flock
          //if to close they will seperate
            for (int i = 0; i < blank; i++)
            {
              
                
                Vector3 poser = pos[i].pos;
                Quaternion head = pos[i].head;
                Vector3 posercheck = poser - post;
                

                Vector3 rele = poser - post;
                //are they on right or lefft
                float matresx = (Vector3.Dot(right.normalized, rele ));
               
              
               
                bool mover = (Vector3.Dot(forw, posercheck) > 0.4f && (posercheck).sqrMagnitude <= Mathf.Pow(8, 2));
                
                
                ++count;
                posupdate += poser*effet*Convert.ToInt16(mover);
                fowupdate += (head*Vector3.forward)*effet*Convert.ToInt16(mover);
                
                bool seperator = ((posercheck).sqrMagnitude <= Mathf.Pow(Mathf.Lerp(18, 10f, effet), 2));
                    
                       
                
                bool eval = (matresx > 0);
                int eval1 = Convert.ToInt16(eval) * 2 - 1;
                sep += (player[blank].rot*Vector3.left)*Mathf.Lerp(40,25, effet)*(eval1*Convert.ToInt16(seperator));


                
               

            }

            for (int i = blank+1 ; i < pos.Length; i++)
            {
                
                
                Vector3 poser = pos[i].pos;
                Quaternion head = pos[i].head;
                Vector3 posercheck = poser - post;
                

                Vector3 rele = poser - post;
                float matresx = (Vector3.Dot(right.normalized, rele ));
               
              
                bool mover = (Vector3.Dot(forw, posercheck) > 0.45f && (posercheck).sqrMagnitude <= Mathf.Pow(8, 2));
                
                    ++count;
                    posupdate += poser*effet*Convert.ToInt16(mover);
                    fowupdate += (head*Vector3.forward)*effet*Convert.ToInt16(mover);
                    bool seperator = ((posercheck).sqrMagnitude <= Mathf.Pow(Mathf.Lerp(30, 23, effet), 2));
                    
                        bool eval = (matresx > 0);
                        int eval1 = Convert.ToInt16(eval) * 2 - 1;
                        sep += (player[blank].rot*Vector3.left)*Mathf.Lerp(50,30, effet)*(eval1*Convert.ToInt16(seperator));

                    
            
                   

                
                
                
            }

            Vector3 g = (target - post)*effet;
             
         
            
            //if in a flock add the effects of the average heading and direction
            if(count != 0)
            {
               
                posupdate /= count;
                fowupdate = fowupdate / count;
                
                
           
                g += (fowupdate +  (posupdate - post).normalized*effet)+sep;
            }

            psedotrans tmp2 = new psedotrans() { pos = player[blank].pos, rot = player[blank].rot };
            Vector3 tmp = Vector3.RotateTowards(forw, new Vector3(g.x,forw.y,g.z), time, time);
            tmp2.rot = Quaternion.LookRotation(tmp,Vector3.up);
        
          tmp2.pos +=time * (tmp2.rot*Vector3.forward).normalized*Mathf.Lerp(20,13, effet);
       
       
            tmp2.pos = new Vector3(nfmod(tmp2.pos.x, 512), tmp2.pos.y, nfmod(tmp2.pos.z, 512));
           
            player[blank] = tmp2;
           
            Boid tmp1 = new Boid();
            tmp1.pos = tmp2.pos;
            tmp1.head = tmp2.rot;
            pos[blank] = tmp1;
            locforbug tmp3 = new locforbug();
            tmp3.pos = new float2(tmp2.pos.x, tmp2.pos.z);
            
            
            float a = ((Vector3.Dot(Vector3.forward , (tmp2.rot*Vector3.forward).normalized)+1)/2);
            float b = Mathf.Lerp(0.5f, 0, a);
            //since 0.5 in turns backwards 0 will become 0.5 and 1 will be 1/0

            // from the dot product i can tell if its facing forward then from the  y component of the cross product
            //or in 2d the Wedge product. I can tell if its left or right.
            
            
            float rotaionT = Mathf.Abs(((Mathf.Sign((Vector3.Cross(Vector3.forward, (tmp2.rot * Vector3.forward).normalized)).y)+1)/2)-b);

            tmp3.rot =rotaionT;
            //calculating these here to send to compute shader
            locer[blank] = tmp3;

        }




    }



}
