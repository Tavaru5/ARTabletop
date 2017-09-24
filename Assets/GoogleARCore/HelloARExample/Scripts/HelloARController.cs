//-----------------------------------------------------------------------
// <copyright file="HelloARController.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.HelloAR
{
    using System.Collections.Generic;
	using System.Collections;
    using UnityEngine;
	using UnityEngine.Rendering;
    using GoogleARCore;
    using SimpleJSON;
    using UnityEngine.Networking;

    /// <summary>
    /// Controlls the HelloAR example.
    /// </summary>
    public class HelloARController : MonoBehaviour
    {

		public int sizeX, sizeZ;

        public GameObject characterPrefab;
        public GameObject wallPrefab;

		private GameObject[,] cells;
        private bool setup = false;

        //Variables for determining proper pathing
        private int fromX = 0;
        private int fromZ = 0;
        private GameObject movingChar;
        List<List<Tuple<int, int>>> coordSets;

		string campaignId;
        int count = 0;

        List<GameObject> characters = new List<GameObject>();
        List<string> userIds = new List<string>();
        List<GameObject> wallIds = new List<GameObject>();

        /// <summary>
        /// The first-person camera being used to render the passthrough camera.
        /// </summary>
        public Camera m_firstPersonCamera;

        /// <summary>
        /// A prefab for tracking and visualizing detected planes.
        /// </summary>
        public GameObject m_trackedPlanePrefab;

        /// <summary>
        /// A model to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject m_andyAndroidPrefab;

        /// <summary>
        /// A gameobject parenting UI for displaying the "searching for planes" snackbar.
        /// </summary>
        public GameObject m_searchingForPlaneUI;

        private List<TrackedPlane> m_newPlanes = new List<TrackedPlane>();

        private List<TrackedPlane> m_allPlanes = new List<TrackedPlane>();

		private GameObject andyObject;

        private Color[] m_planeColors = new Color[] {
            new Color(1.0f, 1.0f, 1.0f),
            new Color(0.956f, 0.262f, 0.211f),
            new Color(0.913f, 0.117f, 0.388f),
            new Color(0.611f, 0.152f, 0.654f),
            new Color(0.403f, 0.227f, 0.717f),
            new Color(0.247f, 0.317f, 0.709f),
            new Color(0.129f, 0.588f, 0.952f),
            new Color(0.011f, 0.662f, 0.956f),
            new Color(0f, 0.737f, 0.831f),
            new Color(0f, 0.588f, 0.533f),
            new Color(0.298f, 0.686f, 0.313f),
            new Color(0.545f, 0.764f, 0.290f),
            new Color(0.803f, 0.862f, 0.223f),
            new Color(1.0f, 0.921f, 0.231f),
            new Color(1.0f, 0.756f, 0.027f)
        };

		public void Start()
		{
			StartCoroutine(GetCampaign("59c6e98c945f7c44deb85b48"));
		}

        IEnumerator GetCampaign(string campaignId)
        {
            //Get and parse data for the campaign
            string url = "http://artabletop.us-east-2.elasticbeanstalk.com/api/campaigns/" + campaignId + "/complete";
            WWW www = new WWW(url);
            yield return www;
            var data = JSON.Parse(www.text);

            //Extract the data about the campaign
            this.campaignId = data["campaign"]["_id"];

            //Extract data for every character in the campaign
            for(int i = 0; i < data["characters"].Count; i ++)
            {
                //Get the variables about the character
                float x = (float)data["characters"][i]["loc_x"];
                float z = (float)data["characters"][i]["loc_z"];
                int health = data["characters"][i]["health"];
                int movement = data["characters"][i]["movement"];

                //Determine where to initially place the character
                Vector3 pos = cells[(int)x, (int)z].transform.position;
                pos = pos + new Vector3 (0, 0.025f, 0);
                GameObject char1 = Instantiate(characterPrefab, pos, Quaternion.Euler(0, 180f, 0));

                //Set the variables for the character
                char1.GetComponent<CharacterRunner>().characterId = data["characters"][i]["_id"];
                char1.GetComponent<CharacterRunner>().health = health;
                char1.GetComponent<CharacterRunner>().movement = movement;
                char1.GetComponent<CharacterRunner>().x = (int)x;
                char1.GetComponent<CharacterRunner>().z = (int)z;
                characters.Add(char1);
            }
            for(int i = 0; i < data["campaign"]["userIds"].Count; i ++)
            {
                userIds.Add(data["campaign"]["userIds"][i]);
            }
            for(int i = 0; i < data["walls"].Count; i ++)
            {
                //Get the data about the walls
                float startX = (float)data["walls"][i]["startX"];
                float startZ = (float)data["walls"][i]["startZ"];
                float endX = (float)data["walls"][i]["endX"];
                float endZ = (float)data["walls"][i]["endZ"];

                //Plan where to place the walls
                float diffX = (endX - startX) * 0.05f;
                float diffZ = (endZ - startZ) * 0.05f;
                Vector3 pos = cells[(int)startX, (int)startZ].transform.position;
                pos = pos + new Vector3 (diffX, 0.075f, diffZ);

                float floatate = 180f;
                
                if(Mathf.Abs(diffX) > Mathf.Abs(diffZ))
                {
                    floatate = 270f;
                }

                GameObject wall1 = Instantiate(wallPrefab, pos, Quaternion.Euler(180f, floatate, 0));
                wall1.GetComponent<WallRunner>().wallId = data["walls"][i]["_id"];
                wall1.GetComponent<WallRunner>().startX = startX;
                wall1.GetComponent<WallRunner>().startZ = startZ;
                wall1.GetComponent<WallRunner>().endX = endX;
                wall1.GetComponent<WallRunner>().endZ = endZ;
                wallIds.Add(wall1);
            }
        }

        IEnumerator GetUpdatedCharacters()
        {
            for(int i = 0; i < characters.Count; i++)
            {
                //Get and parse data for this character
                string url = "http://artabletop.us-east-2.elasticbeanstalk.com/api/characters/" + characters[i].GetComponent<CharacterRunner>().characterId;
                WWW www = new WWW(url);
                yield return www;
                var data = JSON.Parse(www.text);

                //Extract the variables we care about
                float x = (float)data["character"]["loc_x"];
                float z = (float)data["character"]["loc_z"];
                int health = data["character"]["health"];
                int movement = data["character"]["movement"];

                //Get the right coordinates to send the character to
                Vector3 pos = cells[(int)x, (int)z].transform.position;
                pos = pos + transform.position;
                pos = pos + new Vector3 (0, 0.025f, 0);

                //Set the variables for this character
                characters[i].GetComponent<CharacterRunner>().health = health;
                characters[i].GetComponent<Transform>().position = pos;
                characters[i].GetComponent<CharacterRunner>().movement = movement;
                characters[i].GetComponent<CharacterRunner>().x = (int)x;
                characters[i].GetComponent<CharacterRunner>().z = (int)z;

                //Don't hijack a movement if it's in process
                if(!characters[i].GetComponent<CharacterRunner>().moving)
                {
                    characters[i].GetComponent<Transform>().position = pos;
                }
            }
        }

        IEnumerator PostUpdatedCharacters(GameObject character)
        {
            WWWForm form = new WWWForm();
            form.AddField("x", character.GetComponent<CharacterRunner>().x.ToString());
            form.AddField("z", character.GetComponent<CharacterRunner>().z.ToString());
            form.AddField("health", character.GetComponent<CharacterRunner>().health);
            form.AddField("movement", 20);

            Dictionary<string, string> header = new Dictionary<string, string>(){};

            header.Add("Content-Type", "application/x-www-form-urlencoded");
            WWW www = new WWW("http://artabletop.us-east-2.elasticbeanstalk.com/api/characters/" + character.GetComponent<CharacterRunner>().characterId, form.data, header);

            yield return www;
        }

		public void CreateCell(int x, int z, Vector3 point, Transform transf, TrackableHit hit)
		{
			GameObject newCell = Instantiate(m_andyAndroidPrefab, point, Quaternion.identity, transf);
			cells[x,z] = newCell;
			newCell.name = "Maze Cell " + x + ", " + z;
			newCell.transform.parent = transf;
            newCell.GetComponent<TileRunner>().x = x;
            newCell.GetComponent<TileRunner>().z = z;
			newCell.transform.localPosition = new Vector3((x - sizeX) * 0.102f + 0.102f, 0f, (z - sizeZ) * 0.102f + 0.102f);

            // Use a plane attachment component to maintain the cell's y-offset from the plane
            // (occurs after anchor updates).
            newCell.GetComponent<PlaneAttachment>().Attach(hit.Plane);
		}

		public void UpdateCell(int x, int z, Vector3 point, Transform transf, TrackableHit hit)
		{
			cells [x, z].transform.parent = transf;
			cells [x, z].transform.position = point;
			cells [x, z].transform.localPosition = new Vector3 ((x - sizeX) * 0.102f + 0.102f, 0f, (z - sizeZ) * 0.102f + 0.102f);
		}

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update ()
        {
            _QuitOnConnectionErrors();

            // Update the positions of the characters roughly every second
            count ++;
			if(count > 100 && setup)
            {
                count = 0;
                StartCoroutine(GetUpdatedCharacters());
            }

            // The tracking state must be FrameTrackingState.Tracking in order to access the Frame.
            if (Frame.TrackingState != FrameTrackingState.Tracking)
            {
                const int LOST_TRACKING_SLEEP_TIMEOUT = 15;
                Screen.sleepTimeout = LOST_TRACKING_SLEEP_TIMEOUT;
                return;
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Frame.GetNewPlanes(ref m_newPlanes);

            // Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
            for (int i = 0; i < m_newPlanes.Count; i++)
            {
                // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
                // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
                // coordinates.
                GameObject planeObject = Instantiate(m_trackedPlanePrefab, Vector3.zero, Quaternion.identity,
                    transform);
                planeObject.GetComponent<TrackedPlaneVisualizer>().SetTrackedPlane(m_newPlanes[i]);

                // Apply a random color and grid rotation.
                planeObject.GetComponent<Renderer>().material.SetColor("_GridColor", m_planeColors[Random.Range(0,
                    m_planeColors.Length - 1)]);
                planeObject.GetComponent<Renderer>().material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));
            }

            // Disable the snackbar UI when no planes are valid.
            bool showSearchingUI = true;
            Frame.GetAllPlanes(ref m_allPlanes);
            for (int i = 0; i < m_allPlanes.Count; i++)
            {
                if (m_allPlanes[i].IsValid)
                {
                    showSearchingUI = false;
                    break;
                }
            }

            m_searchingForPlaneUI.SetActive(showSearchingUI);

			Touch touch = Input.GetTouch (0);
			if (Input.touchCount < 1 || touch.phase != TouchPhase.Began)
            {
                return;
            }

			RaycastHit arHit;

            TrackableHit hit;
            TrackableHitFlag raycastFilter = TrackableHitFlag.PlaneWithinBounds | TrackableHitFlag.PlaneWithinPolygon;

			Ray ray = m_firstPersonCamera.ScreenPointToRay (touch.position);

            //If the click by the user landed on an existing object
			if (Physics.Raycast(ray, out arHit, Mathf.Infinity)) 
			{
                GameObject hitObject = arHit.collider.gameObject;
                int hitX = hitObject.GetComponent<TileRunner>().x;
                int hitZ = hitObject.GetComponent<TileRunner>().z;
                bool alreadyMoving = false;
                int existingX = -1;
                int existingZ = -1;

                //Determine if we we're already trying to move from somewhere
                for(int i = 0; i < cells.GetLength(0); i ++)
                {
                    for(int j = 0; j < cells.GetLength(1); j++)
                    {
                        if(cells[i, j].GetComponent<TileRunner>().isSelected == true)
                        {
                            alreadyMoving = true;
                            existingX = i;
                            existingZ = j;
                            break;
                        }
                    }
                    if(alreadyMoving)
                    {
                        break;
                    }
                }

                //There was already a selected tile, so move that character to here.
                if(alreadyMoving)
                {
                    //Look for the character from the old point
                    GameObject character = null;
                    for(int i = 0; i < characters.Count; i ++)
                    {
                        if(characters[i].GetComponent<CharacterRunner>().x == existingX && characters[i].GetComponent<CharacterRunner>().z == existingZ)
                        {
                            character = characters[i];
                            break;
                        }
                    }
                    //We found the character that needs to be moved, so move it
                    if(character != null)
                    {
                        StartCoroutine(ClearPaths());
                        character.GetComponent<CharacterRunner>().x = hitX;
                        character.GetComponent<CharacterRunner>().z = hitZ;
                        StartCoroutine(PostUpdatedCharacters(character));
                        character.GetComponent<CharacterRunner>().moving = false;
                        cells[existingX, existingZ].GetComponent<TileRunner>().selected(false);
                    }
                }
                //A tile hasn't already been selected, select this one to move from
                else
                {
                    //Check to make sure a character is on the spot that was selected
                    bool charExists = false;
                    GameObject character = null;
                    for(int i = 0; i < characters.Count; i ++)
                    {
                        if(characters[i].GetComponent<CharacterRunner>().x == hitX && characters[i].GetComponent<CharacterRunner>().z == hitZ)
                        {
                            charExists = true;
                            break;
                        }
                    }
                    //Save the details about the potential move to show the path lines
                    if(charExists)
                    {
                        fromX = hitX;
                        fromZ = hitZ;

                        coordSets = new List<List<Tuple<int, int>>>();

                        if(character != null)
                        {
                            movingChar = character;
                            character.GetComponent<CharacterRunner>().moving = true;
                        }
                        hitObject.GetComponent<TileRunner>().selected(true);

                        //Start calculating the character's path options
                        StartCoroutine(ShowPaths());
                    }
                }
			}
            //The users's click landed on the ARField behind the objects
            else if (Session.Raycast(m_firstPersonCamera.ScreenPointToRay(touch.position), raycastFilter, out hit))
            {
                //Only go through the setup on the first tap on ARField
				if(!setup)
                {
                    setup = true;

                    // Create an anchor to allow ARCore to track the hitpoint as understanding of the physical
                    // world evolves.
                    var anchor = Session.CreateAnchor(hit.Point, Quaternion.identity);

                    cells = new GameObject[sizeX, sizeZ];

                    for (int x = 0; x < sizeX; x++) 
                    {
                        for (int z = 0; z < sizeZ; z++) 
                        {
                            CreateCell(x, z, hit.Point, anchor.transform, hit);
                        }
                    }

                    StartCoroutine(GetCampaign("59c6e98c945f7c44deb85b48"));
                }
                //The BattleMat already exists, user is just trying to move it
                else
                {
					var anchor = Session.CreateAnchor(hit.Point, Quaternion.identity);
					for (int x = 0; x < sizeX; x++) 
					{
						for (int z = 0; z < sizeZ; z++) 
						{
							UpdateCell(x, z, hit.Point, anchor.transform, hit);
						}
					}
                }
            }

            //Update the walls to follow the floors
            for(int i = 0; i < wallIds.Count; i++)
            {
                //Get the data about the walls
                float startX = (float)wallIds[i].GetComponent<WallRunner>().startX;
                float startZ = (float)wallIds[i].GetComponent<WallRunner>().startZ;
                float endX = (float)wallIds[i].GetComponent<WallRunner>().endX;
                float endZ = (float)wallIds[i].GetComponent<WallRunner>().endZ;

                //Plan where to place the walls
                float diffX = (endX - startX) * 0.05f;
                float diffZ = (endZ - startZ) * 0.05f;
                Vector3 pos = cells[(int)startX, (int)startZ].transform.position;
                pos = pos + new Vector3 (diffX, 0.075f, diffZ);

                wallIds[i].GetComponent<Transform>().position = pos;
            }
        }

        //Use #GRAPHTHEORY to determine all possible paths away from the point
        IEnumerator ShowPaths()
        {
            int steps = 0;
            //Debug.Log("Move count is " + (movingChar.GetComponent<CharacterRunner>().movement / 5));
            //Find all valid movement spaces
            Debug.Log("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n");
            while(steps < 4)
            {
                Debug.Log("Step is " + steps);
                List<Tuple<int, int>> newCoords = new List<Tuple<int, int>>();
                List<Tuple<int, int>> lastCoords;

                //Determine what the last group of coordinates were in order to branch out
                if(steps == 0)
                {
                    lastCoords = new List<Tuple<int, int>>();
                    lastCoords.Add(new Tuple<int, int>(fromX, fromZ));
                }
                else
                {
                    lastCoords = coordSets[steps - 1];
                }

                //For every last possible coord, look all around it for possible coords
                for(int i = 0; i < lastCoords.Count; i ++)
                {
                    Debug.Log("Based on point " + lastCoords[i].First + ", " + lastCoords[i].Second);
                    for(int x = lastCoords[i].First - 1; x < lastCoords[i].First + 2; x ++)
                    {
                        for(int z = lastCoords[i].Second - 1; z < lastCoords[i].Second + 2; z ++)
                        {
                            Debug.Log("Considering point " + x + ", " + z);
                            //Don't re-add the point we're checking from
                            if(x == lastCoords[i].First && z == lastCoords[i].Second)
                            {
                                break;
                            }

                            //Determine if the point in question is allowed (no wall, on map, no other char)
                            Tuple<int, int> testPoint = new Tuple<int, int>(x, z);
                            if(x >= 0 && z >= 0 && x < sizeX && z < sizeZ)
                            {
                                //Make sure the point isn't already in the existing set.
                                bool flag = false;

                                //Make sure the point hasn't just been tested
                                for(int j = 0; j < newCoords.Count; j++)
                                {
                                    if(newCoords[j] == testPoint)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }

                                //Make sure there isn't a wall inbetween the testPoint and the currentPoint
                                for(int k = 0; k < wallIds.Count; k++)
                                {
                                    //Get the two sides of the wall
                                    int startX = (int)wallIds[k].GetComponent<WallRunner>().startX;
                                    int startZ = (int)wallIds[k].GetComponent<WallRunner>().startZ;
                                    int endX = (int)wallIds[k].GetComponent<WallRunner>().endX;
                                    int endZ = (int)wallIds[k].GetComponent<WallRunner>().endZ;

                                    if((lastCoords[i].First == startX && lastCoords[i].Second == startZ && x == endX && z == endZ) || (lastCoords[i].First == endX && lastCoords[i].Second == endZ && x == startX && z == startZ))
                                    {
                                        flag = true;
                                        break;
                                    }
                                }

                                if(!flag)
                                {
                                    Debug.Log("Adding point!");
                                    newCoords.Add(new Tuple<int, int>(x, z));
                                }
                            }
                        }
                    }
                }

                coordSets.Add(newCoords);
                steps ++;
            }


            //Highlight all previously found movement spaces
            for(int i = 0; i < coordSets.Count; i ++)
            {
                for(int j = 0; j < coordSets[i].Count; j++)
                {
                    cells[coordSets[i][j].First, coordSets[i][j].Second].GetComponent<TileRunner>().highlight();
                }
            }

            yield return "success";
        }

        //Set every tile back to its original texture
        IEnumerator ClearPaths()
        {
            for(int i = 0; i < cells.GetLength(0); i ++)
            {
                for(int j = 0; j < cells.GetLength(1); j++)
                {
                    cells[i, j].GetComponent<TileRunner>().selected(false);
                }
            }

            yield return "success";
        }

        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors()
        {
            // Do not update if ARCore is not tracking.
            if (Session.ConnectionState == SessionConnectionState.DeviceNotSupported)
            {
                _ShowAndroidToastMessage("This device does not support ARCore.");
                Application.Quit();
            }
            else if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                Application.Quit();
            }
            else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed)
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                Application.Quit();
            }
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        /// <param name="length">Toast message time length.</param>
        private static void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }
    }

    public class Tuple<T1, T2>
    {
        public T1 First { get; private set; }
        public T2 Second { get; private set; }
        internal Tuple(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }
    
    public static class Tuple
    {
        public static Tuple<T1, T2> New<T1, T2>(T1 first, T2 second)
        {
            var tuple = new Tuple<T1, T2>(first, second);
            return tuple;
        }
    }
}
