using UnityEngine;
public class Main : MonoBehaviour
{
    // ! STRUCTS
    struct Agent
    {
        public Vector2 pos;
        public float angle;
        public float speed;
        public float turnSpeed;
        public float sensorSize;
        public float sensorAngOffset;
        public float sensorDtsOffset;
        public float depositRate;
        public Vector4 speciesMask;
    }

    struct Parameters
    {
        public int numAgents;
        public float evaporateSpeed;
        public int blurOffset;
        public float diffuseSpeed;
        public float speed;
        public float turnSpeed;
        public float sensorSize;
        public float sensorAngOffset;
        public float sensorDtsOffset;
    }

    // ! VARIABLES
    public ComputeShader computeShader;
    int agentsSensorKernel;
    int agentsMoveKernel;
    int screenUpdateKernel;
    int screenClearKernel;

    private RenderTexture screen;

    private ComputeBuffer agents;

    // ! SIMULATION PARAMETERS
    private int numAgents;
    private float evaporateSpeed;
    private int blurOffset;
    private float diffuseSpeed;


    private void Start()
    {
        print("Start");
        InitSystemVariables();
        InitRenderTexture();
        ClearScreen();
        InitSimulation();
    }

    private void OnApplicationFocus()
    {
        print("OnApplicationFocus");
        InitSystemVariables();
        InitRenderTexture();
        ClearScreen();
        InitSimulation();
    }

    private void InitSimulation()
    {
        print("InitSimulation");
        // numAgents, evaporateSpeed, blurOffset, diffuseSpeed, agenteSpeed, turnSpeed, sensorSize, sensorAngOffset, sensorDtsOffset)
        Parameters[] parameters = new Parameters[]{
            // * 0 * //
            new Parameters{
                numAgents = 150000, evaporateSpeed = 0.3f, blurOffset = 1, diffuseSpeed = 50.0f,
                speed = 500.0f, turnSpeed = 100.0f, sensorSize = 2.0f, sensorAngOffset = Radians(22.5f), sensorDtsOffset = 20.0f
            },
            // * 1 *//
            new Parameters{
                numAgents = 75000, evaporateSpeed = 0.5f, blurOffset = 1, diffuseSpeed = 100.0f,
                speed = 500.0f, turnSpeed = 1000.0f, sensorSize = 2.0f, sensorAngOffset = Radians(45.0f), sensorDtsOffset = 100.0f
            },
            // * 2 * //
            new Parameters{
                numAgents = Mathf.FloorToInt(1280f*720f*0.15f), evaporateSpeed = 1.0f, blurOffset = 1, diffuseSpeed = 0.1f,
                speed = 100.0f, turnSpeed = 100.0f, sensorSize = 1.0f, sensorAngOffset = Radians(22.5f), sensorDtsOffset = 9.0f
            },
            // * 3 * //
            new Parameters{
                numAgents = Mathf.FloorToInt(720*720*0.1f), evaporateSpeed = 0.5f, blurOffset = 1, diffuseSpeed = 1,
                speed = 250, turnSpeed = 20, sensorSize = 2, sensorAngOffset = Radians(5.0f), sensorDtsOffset = 35
            },
            // * 4 * //
            new Parameters{
                numAgents = 100000, evaporateSpeed = 1.0f, blurOffset = 1, diffuseSpeed = 5f,
                speed = 100.0f, turnSpeed = 45, sensorSize = 1, sensorAngOffset = Radians(90.0f), sensorDtsOffset = 100
            },
            // * 5 * //
            new Parameters{
                numAgents = 200000, evaporateSpeed = 0.5f, blurOffset = 1, diffuseSpeed = 5f,
                speed = 250.0f, turnSpeed = 50.0f, sensorSize = 1, sensorAngOffset = Radians(10.0f), sensorDtsOffset = 50.0f
            },
            // * 6 * //
            new Parameters{
                numAgents = Mathf.FloorToInt(512*512*0.5f), evaporateSpeed = 2.0f, blurOffset = 1, diffuseSpeed = 50,
                speed = 50, turnSpeed = 5, sensorSize = 0, sensorAngOffset = Radians(90.0f), sensorDtsOffset = 3
            }
        };

        Parameters param = parameters[6];
        numAgents = param.numAgents;
        evaporateSpeed = param.evaporateSpeed;
        blurOffset = param.blurOffset;
        diffuseSpeed = param.diffuseSpeed;

        computeShader.SetInt("numAgents", numAgents);
        computeShader.SetFloat("evaporateSpeed", evaporateSpeed);
        computeShader.SetInt("blurOffset", blurOffset);
        computeShader.SetFloat("diffuseSpeed", diffuseSpeed);

        Agent[] agentsArray = new Agent[numAgents];
        for (uint i = 0; i < numAgents; i++)
        {
            //agentsArray[i].pos = new Vector2(Random.Range(Screen.width * 0.05f, Screen.width * 0.95f), Random.Range(Screen.height * 0.05f, Screen.height * 0.95f));
            agentsArray[i].pos = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
            //agentsArray[i].vel = new Vector2(Random.Range(-Screen.width / 4, Screen.width / 4), Random.Range(-Screen.width / 4, Screen.width / 4));
            agentsArray[i].angle = Random.Range(0.0f, 2.0f * Mathf.PI);
            //agentsArray[i].vel = Random.Range(0.0f, Screen.width / 4.0f);
            agentsArray[i].speed = param.speed;
            agentsArray[i].turnSpeed = param.turnSpeed;
            agentsArray[i].sensorSize = param.sensorSize;
            agentsArray[i].sensorAngOffset = param.sensorAngOffset;
            agentsArray[i].sensorDtsOffset = param.sensorDtsOffset;
            agentsArray[i].depositRate = 5.0f;
            int speciesIndex = Mathf.FloorToInt(Random.Range(0, 3));
            if (speciesIndex == 0)
            {
                agentsArray[i].speciesMask = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            }
            else if (speciesIndex == 1)
            {
                agentsArray[i].speciesMask = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            }
            else if (speciesIndex == 2)
            {
                agentsArray[i].speciesMask = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
            }
        }
        agents = new ComputeBuffer(numAgents, 13 * 4);
        agents.SetData(agentsArray);
    }

    private void Update()
    {
        print("Update");
        if (Time.time < 1.0) return;
        if (InitRenderTexture())
        {
            ClearScreen();
            InitSimulation();
        }

        computeShader.SetFloat("time", Time.time);
        computeShader.SetFloat("deltaTime", Time.deltaTime);

        computeShader.SetTexture(agentsSensorKernel, "screen", screen);
        computeShader.SetBuffer(agentsSensorKernel, "agents", agents);
        computeShader.Dispatch(agentsSensorKernel, Mathf.CeilToInt(numAgents / 100.0f), 1, 1);

        computeShader.SetTexture(agentsMoveKernel, "screen", screen);
        computeShader.SetBuffer(agentsMoveKernel, "agents", agents);
        computeShader.Dispatch(agentsMoveKernel, Mathf.CeilToInt(numAgents / 100.0f), 1, 1);

        computeShader.SetTexture(screenUpdateKernel, "screen", screen);
        computeShader.Dispatch(screenUpdateKernel, Mathf.CeilToInt(Screen.width / 16.0f), Mathf.CeilToInt(Screen.height / 16.0f), 1);
    }

    private void InitSystemVariables()
    {
        print("InitSystemVariables");
        agentsSensorKernel = computeShader.FindKernel("agentsSensorKernel");
        agentsMoveKernel = computeShader.FindKernel("agentsMoveKernel");
        screenUpdateKernel = computeShader.FindKernel("screenUpdateKernel");
        screenClearKernel = computeShader.FindKernel("screenClearKernel");
    }

    private bool InitRenderTexture()
    {
        if (screen == null || screen.width != Screen.width || screen.height != Screen.height)
        {
            print("InitRenderTexture");
            // Release render texture if we already have one
            if (screen != null)
                screen.Release();

            // Get a render target for Ray Tracing
            screen = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            screen.enableRandomWrite = true;
            screen.Create();

            computeShader.SetInt("screenWidth", Screen.width);
            computeShader.SetInt("screenHeight", Screen.height);

            return true;
        }
        return false;
    }

    private void ClearScreen()
    {
        print("ClearScreen");
        computeShader.SetTexture(screenClearKernel, "screen", screen);
        computeShader.Dispatch(screenClearKernel, Mathf.CeilToInt(Screen.width / 16.0f), Mathf.CeilToInt(Screen.height / 16.0f), 1);
    }

    private float Radians(float deg)
    {
        return deg * Mathf.PI / 180.0f;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        print("OnRenderImage");
        Graphics.Blit(screen, destination);
    }
}