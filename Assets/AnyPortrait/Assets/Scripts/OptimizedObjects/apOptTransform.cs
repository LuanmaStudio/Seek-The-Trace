﻿/*
*	Copyright (c) 2017-2020. RainyRizzle. All rights reserved
*	Contact to : https://www.rainyrizzle.com/ , contactrainyrizzle@gmail.com
*
*	This file is part of [AnyPortrait].
*
*	AnyPortrait can not be copied and/or distributed without
*	the express perission of [Seungjik Lee].
*
*	Unless this file is downloaded from the Unity Asset Store or RainyRizzle homepage, 
*	this file and its users are illegal.
*	In that case, the act may be subject to legal penalties.
*/

using UnityEngine;
//using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;
using System;


using AnyPortrait;

namespace AnyPortrait
{

	//에디터의 apRenderUnit + [Transform_Mesh/Transform_MeshGroup]이 합쳐진 실행객체
	//Transform (Mesh/MG) 데이터와 RenderUnit의 Update 기능들이 여기에 모두 포함된다.
	/// <summary>
	/// This is a class that belongs hierarchically under "apOptRootUnit".
	/// This is the target of the modifier, has bones or contains meshes.
	/// (Although it can be controlled by a script, it takes precedence to process it from Modifier.)
	/// </summary>
	public class apOptTransform : MonoBehaviour
	{
		// Members
		//------------------------------------------------
		/// <summary>[Please do not use it] Parent Portrait</summary>
		public apPortrait _portrait = null;

		/// <summary>[Please do not use it] Parent Root Unit</summary>
		public apOptRootUnit _rootUnit = null;

		/// <summary>[Please do not use it] Unique ID</summary>
		public int _transformID = -1;

		/// <summary>[Please do not use it] Transform Name</summary>
		public string _name = "";


		/// <summary>[Please do not use it] Transform</summary>
		[HideInInspector]
		public Transform _transform = null;

		/// <summary>[Please do not use it]</summary>
		[SerializeField]
		public apMatrix _defaultMatrix;


		//RenderUnit 데이터
		
		public enum UNIT_TYPE { Group = 0, Mesh = 1 }

		/// <summary>[Please do not use it] Unit Type (MeshGroup / Mesh)</summary>
		public UNIT_TYPE _unitType = UNIT_TYPE.Group;

		/// <summary>[Please do not use it] UniqueID of MeshGroup</summary>
		public int _meshGroupUniqueID = -1;//Group 타입이면 meshGroupUniqueID를 넣어주자.

		/// <summary>[Please do not use it] Hierarchy Level</summary>
		public int _level = -1;

		/// <summary>[Please do not use it] Z Depth Value</summary>
		public int _depth = -1;

		/// <summary>[Please do not use it]</summary>
		[SerializeField]
		public bool _isVisible_Default = true;

		/// <summary>[Please do not use it]</summary>
		[SerializeField]
		public Color _meshColor2X_Default = Color.gray;


		/// <summary>Parent Opt-Transform</summary>
		public apOptTransform _parentTransform = null;

		/// <summary>Number of Children Opt-Transform</summary>
		public int _nChildTransforms = 0;

		/// <summary>Chilndren Array</summary>
		public apOptTransform[] _childTransforms = null;

		//Mesh 타입인 경우
		/// <summary>Opt Mesh (if it is MeshType)</summary>
		public apOptMesh _childMesh = null;//실제 Mesh MonoBehaviour
		
		//<참고>
		//원래 apRenderVertex는 renderUnit에 있지만, 여기서는 apOptMesh에 직접 포함되어 있다.



		//Modifier의 값을 전달받는 Stack
		[NonSerialized]
		private apOptCalculatedResultStack _calculatedStack = null;

		public apOptCalculatedResultStack CalculatedStack
		{
			get
			{
				if (_calculatedStack == null)
				{ _calculatedStack = new apOptCalculatedResultStack(this); }
				return _calculatedStack;
			}
		}

		/// <summary>[Please do not use it] It stores Modifiers</summary>
		[SerializeField]
		public apOptModifierSubStack _modifierStack = new apOptModifierSubStack();

		//업데이트 되는 변수
		//[NonSerialized]
		//public apMatrix3x3 _matrix_TF_Cal_ToWorld = apMatrix3x3.identity;

		//private apMatrix _calculateTmpMatrix = new apMatrix();
		//public apMatrix CalculatedTmpMatrix {  get { return _calculateTmpMatrix; } }


		//World Transform을 구하기 위해선
		// World Transform = [Parent World] x [To Parent] x [Modified]

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public apMatrix _matrix_TF_ParentWorld = new apMatrix();

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public apMatrix _matrix_TF_ParentWorld_NonModified = new apMatrix();

		/// <summary>[Please do not use it] Updated Matrix</summary>
		//Opt Transform은 기본 좌표에 ToParent가 반영되어 있다.
		[NonSerialized]
		public apMatrix _matrix_TF_ToParent = new apMatrix();

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public apMatrix _matrix_TF_LocalModified = new apMatrix();

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public apMatrix _matrix_TFResult_World = new apMatrix();

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public bool _isCalculateWithoutMod = false;//WithoutMod 계열은 한번만 계산한다.

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public apMatrix _matrix_TFResult_WorldWithoutMod = new apMatrix();

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public Color _meshColor2X = new Color(0.5f, 0.5f, 0.5f, 1.0f);

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public bool _isAnyColorCalculated = false;

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public bool _isVisible = false;

		private const float VISIBLE_ALPHA = 0.01f;


		//추가 12.5 : Extra 옵션
		
		[NonSerialized]
		private bool _isExtraTextureChanged = false;
		
		[NonSerialized]
		private apOptTextureData _extraTextureData = null;

		[NonSerialized]
		private bool _isExtraTextureChanged_Prev = false;

		[NonSerialized]
		private apOptTextureData _extraTextureData_Prev = null;

		//삭제 20.4.21
		//[NonSerialized]
		//private Texture2D _extraDefaultTexture = null;//Extra 옵션에 의해서 오버랩 되기 이전의 텍스쳐

		
		public delegate void FUNC_EXTRA_DEPTH_CHANGED(apOptTransform renderUnit, int deltaDepth);
		[NonSerialized]
		private FUNC_EXTRA_DEPTH_CHANGED _func_ExtraDepthChanged = null;





		/// <summary>[Please do not use it] Updated Matrix</summary>
		//Rigging을 위한 단축 식
		[NonSerialized]
		public apMatrix3x3 _vertLocal2MeshWorldMatrix = new apMatrix3x3();
		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public apMatrix3x3 _vertWorld2MeshLocalMatrix = new apMatrix3x3();

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public apMatrix3x3 _vertMeshWorldNoModMatrix = new apMatrix3x3();

		/// <summary>[Please do not use it] Updated Matrix</summary>
		[NonSerialized]
		public apMatrix3x3 _vertMeshWorldNoModInverseMatrix = new apMatrix3x3();







		// OptBone을 추가한다.
		//OptBone의 GameObject가 저장되는 Transform (내용은 없다)
		/// <summary>Root Transform containing the Bones</summary>
		public Transform _boneGroup = null;

		/// <summary>Bones</summary>
		public apOptBone[] _boneList_All = null;

		//추가 19.5.23 : 
		private Dictionary<int, apOptBone> _boneID2Bone = null;


		/// <summary>Bones (Root Only)</summary>
		public apOptBone[] _boneList_Root = null;

		/// <summary>[Please do not use it]</summary>
		public bool _isBoneUpdatable = false;


		//Attach시 만들어지는 Socket
		//Socket 옵션은 MeshTransform/MeshGroupTransform에서 미리 세팅해야한다.
		/// <summary>Socket Transform (it is not null if "Socket Option" is enabled)</summary>
		public Transform _socketTransform = null;

		

		//스크립트로 TRS를 직접 제어할 수 있다.
		//단 Update마다 매번 설정해야한다.
		//좌표계는 WorldMatrix를 기준으로 한다.
		//값 자체는 절대값을 기준으로 한다.
		private bool _isExternalUpdate_Position = false;
		private bool _isExternalUpdate_Rotation = false;
		private bool _isExternalUpdate_Scaling = false;
		private float _externalUpdateWeight = 0.0f;
		private Vector2 _exUpdate_Pos = Vector2.zero;
		private float _exUpdate_Angle = 0.0f;
		private Vector2 _exUpdate_Scale = Vector2.zero;

		//처리된 TRS
		private Vector3 _updatedWorldPos = Vector3.zero;
		private float _updatedWorldAngle = 0.0f;
		private Vector3 _updatedWorldScale = Vector3.one;

		private Vector3 _updatedWorldPos_NoRequest = Vector3.zero;
		private float _updatedWorldAngle_NoRequest = 0.0f;
		private Vector3 _updatedWorldScale_NoRequest = Vector3.one;

		[NonSerialized]
		private bool _isCamOrthoCorrection = false;
		
		[NonSerialized]
		private Matrix4x4 _convert2TargetMatrix4x4 = Matrix4x4.identity;

		[NonSerialized]
		public apMatrix3x3 _convert2TargetMatrix3x3 = apMatrix3x3.identity;

		[NonSerialized]
		private bool _isFlippedRoot_X = false;

		[NonSerialized]
		private bool _isFlippedRoot_Y = false;

		//추가됨 19.5.25
		[SerializeField]
		public bool _isUseModMeshSet = false;


		// Init
		//------------------------------------------------
		void Awake()
		{
			_transform = transform;
		}

		void Start()
		{
			_isExternalUpdate_Position = false;
			_isExternalUpdate_Rotation = false;
			_isExternalUpdate_Scaling = false;

			_isCalculateWithoutMod = false;

			_isExtraTextureChanged_Prev = false;//추가
			_extraTextureData_Prev = null;

			//업데이트 안합니더
			this.enabled = false;
		}


		

		// Update
		//------------------------------------------------
		void Update()
		{

		}

		void LateUpdate()
		{

		}




		// Update (외부에서 업데이트를 한다.)
		//------------------------------------------------
		public void UpdateModifier_Pre(float tDelta)
		{
			if (_modifierStack != null)
			{
				_modifierStack.Update_Pre(tDelta);
			}

			//자식 객체도 업데이트를 한다.
			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].UpdateModifier_Pre(tDelta);
				}
			}
		}

		public void UpdateModifier_Post(float tDelta)
		{

			if (_modifierStack != null)
			{
				_modifierStack.Update_Post(tDelta);
			}

			//자식 객체도 업데이트를 한다.
			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].UpdateModifier_Post(tDelta);
				}
			}
		}




		


		public void ReadyToUpdate()
		{
			//1. Child Mesh와 기본 Reday
			if (_childMesh != null)
			{
				_childMesh.ReadyToUpdate();
			}

			//2. Calculate Stack Ready
			if (_calculatedStack != null)
			{
				_calculatedStack.ReadyToCalculate();
			}

			//3. 몇가지 변수 초기화
			_meshColor2X = new Color(0.5f, 0.5f, 0.5f, 1.0f);
			_isAnyColorCalculated = false;

			_isVisible = true;


			//추가 11.30 : Extra Option
			_isExtraTextureChanged = false;
			_extraTextureData = null;

			//Editor에서는 기본 matrix가 들어가지만, 여기서는 아예 Transform(Mono)에 들어가기 때문에 Identity가 된다.
			//_matrix_TF_Cal_ToWorld = apMatrix3x3.identity;
			//_calculateTmpMatrix.SetIdentity();


			//변경
			//[Parent World x To Parent x Local TF] 조합으로 변경

			if (_matrix_TF_ParentWorld == null)				{ _matrix_TF_ParentWorld = new apMatrix(); }
			if (_matrix_TF_ParentWorld_NonModified == null)	{ _matrix_TF_ParentWorld_NonModified = new apMatrix(); }
			if (_matrix_TF_ToParent == null)				{ _matrix_TF_ToParent = new apMatrix(); }
			if (_matrix_TF_LocalModified == null)			{ _matrix_TF_LocalModified = new apMatrix(); }
			if (_matrix_TFResult_World == null)				{ _matrix_TFResult_World = new apMatrix(); }
			if (_matrix_TFResult_WorldWithoutMod == null)	{ _matrix_TFResult_WorldWithoutMod = new apMatrix(); }


			_matrix_TF_ParentWorld.SetIdentity();
			_matrix_TF_ParentWorld_NonModified.SetIdentity();
			//_matrix_TF_ToParent.SetIdentity();
			_matrix_TF_LocalModified.SetIdentity();

			//Editor에서는 기본 matrix가 들어가지만, 여기서는 아예 Transform(Mono)에 들어가기 때문에 Identity가 된다.
			_matrix_TF_ToParent.SetMatrix(_defaultMatrix);

			_matrix_TFResult_World.SetIdentity();

			if (!_isCalculateWithoutMod)
			{
				_matrix_TFResult_WorldWithoutMod.SetIdentity();
			}

			_isCamOrthoCorrection = false;//<<추가
			

			//3. 자식 호출
			//자식 객체도 업데이트를 한다.
			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].ReadyToUpdate();
				}
			}
		}





		/// <summary>
		/// CalculateStack을 업데이트 한다.
		/// Pre-Update이다. Rigging, VertWorld는 제외된다.
		/// </summary>
		public void UpdateCalculate_Pre()
		{

//#if UNITY_EDITOR
//			Profiler.BeginSample("Transform - 1. Stack Calculate");
//#endif

			//1. Calculated Stack 업데이트
			if (_calculatedStack != null)
			{
				_calculatedStack.Calculate_Pre();
			}

//#if UNITY_EDITOR
//			Profiler.EndSample();
//#endif


//#if UNITY_EDITOR
//			Profiler.BeginSample("Transform - 2. Matrix / Color");
//#endif

			//2. Calculated의 값 적용 + 계층적 Matrix 적용
			if (CalculatedStack.MeshWorldMatrixWrap != null)
			{
				//변경전
				//_calclateTmpMatrix.SRMultiply(_calculatedStack.MeshWorldMatrixWrap, true);
				//_matrix_TF_Cal_ToWorld = _calculateTmpMatrix.MtrxToSpace;

				//변경후
				_matrix_TF_LocalModified.SetMatrix(_calculatedStack.MeshWorldMatrixWrap);

				//if(_calculatedStack.MeshWorldMatrixWrap.Scale2.magnitude < 0.8f)
				//{
				//	Debug.Log(name + " : Low Scale : " + _calculatedStack.MeshWorldMatrixWrap.Scale2);
				//}

			}

			if (CalculatedStack.IsAnyColorCalculated)
			{
				_meshColor2X = CalculatedStack.MeshColor;
				_isVisible = CalculatedStack.IsMeshVisible;
				_isAnyColorCalculated = true;
			}
			else
			{
				_meshColor2X = _meshColor2X_Default;
				_isVisible = _isVisible_Default;
			}
			if (!_isVisible)
			{
				_meshColor2X.a = 0.0f;
				_isAnyColorCalculated = true;
			}


			//추가 11.30 : Extra Option
			if (_calculatedStack.IsExtraDepthChanged)
			{
				if(_func_ExtraDepthChanged != null)
				{
					_func_ExtraDepthChanged(this, _calculatedStack.ExtraDeltaDepth);
				}
			}
			if (_calculatedStack.IsExtraTextureChanged)
			{
				_isExtraTextureChanged = true;
				_extraTextureData = _calculatedStack.ExtraTextureData;
				if(_extraTextureData == null)
				{
					_isExtraTextureChanged = false;
				}
			}

			if(_isExtraTextureChanged_Prev != _isExtraTextureChanged ||
				_extraTextureData_Prev != _extraTextureData)
			{
#if UNITY_EDITOR
				if (Application.isPlaying)
				{
#endif
					//Extra-Texture 이벤트가 발생하거나 해지되어서 상태가 바뀌었을 경우
					//또는 새로운 텍스쳐가 발생했을 경우
					if (_childMesh != null)
					{
						if (_isExtraTextureChanged)
						{
							//> 텍스쳐 교체 이벤트가 발생했다.
							//현재 처리중인 텍스쳐를 먼저 저장한 뒤, 교체
							//이전
							//_extraDefaultTexture = _childMesh.GetCurrentMainTextureExceptExtra();
							//_childMesh.SetExtraChangedTexture(_extraTextureData._texture);

							//변경 20.4.21
							_childMesh.SetExtraChangedTexture(_extraTextureData._texture);
						}
						else
						{
							//>텍스쳐를 해지해야한다. 저장했던 값으로 되돌린다.
							//이전
							//_childMesh.SetExtraChangedTexture(_extraDefaultTexture);
							//Debug.LogError(">> Return Texture [" + _extraDefaultTexture.name + "]");

							//변경 20.4.20 : 복구 함수를 아예 이용하자
							_childMesh.RestoreFromExtraTexture();
						}
					}
#if UNITY_EDITOR
				}
#endif
				_isExtraTextureChanged_Prev = _isExtraTextureChanged;
				if (_isExtraTextureChanged)
				{
					_extraTextureData_Prev = _extraTextureData;
				}
				else
				{
					_extraTextureData_Prev = null;
				}
			}



			if (_parentTransform != null)
			{
				//변경 전
				//_calculateTmpMatrix.SRMultiply(_parentTransform.CalculatedTmpMatrix, true);
				//_matrix_TF_Cal_ToWorld = _calculateTmpMatrix.MtrxToSpace;

				//변경 후
				_matrix_TF_ParentWorld.SetMatrix(_parentTransform._matrix_TFResult_World);
				_matrix_TF_ParentWorld_NonModified.SetMatrix(_parentTransform._matrix_TFResult_WorldWithoutMod);

				//색상은 2X 방식의 Add
				_meshColor2X.r = Mathf.Clamp01(((float)(_meshColor2X.r) - 0.5f) + ((float)(_parentTransform._meshColor2X.r) - 0.5f) + 0.5f);
				_meshColor2X.g = Mathf.Clamp01(((float)(_meshColor2X.g) - 0.5f) + ((float)(_parentTransform._meshColor2X.g) - 0.5f) + 0.5f);
				_meshColor2X.b = Mathf.Clamp01(((float)(_meshColor2X.b) - 0.5f) + ((float)(_parentTransform._meshColor2X.b) - 0.5f) + 0.5f);
				_meshColor2X.a *= _parentTransform._meshColor2X.a;

				if(_parentTransform._isAnyColorCalculated)
				{
					_isAnyColorCalculated = true;
				}
			}

			if (_meshColor2X.a < VISIBLE_ALPHA
				//|| !CalculatedStack.IsMeshVisible
				)
			{
				_isVisible = false;
				_meshColor2X.a = 0.0f;
				_isAnyColorCalculated = true;
			}

			//MakeTransformMatrix(); < 이 함수 부분
			//World Matrix를 만든다.
			_matrix_TFResult_World.RMultiply(_matrix_TF_ToParent);//변경 : ToParent -> LocalModified -> ParentWorld 순으로 바꾼다.
			_matrix_TFResult_World.RMultiply(_matrix_TF_LocalModified);//<<[R]


			//_matrix_TFResult_World.RMultiply(_matrix_TF_ToParent);//<<[R]

			_matrix_TFResult_World.RMultiply(_matrix_TF_ParentWorld);//<<[R]

			//추가 2.25 : Flipped
			if(_isFlippedRoot_X || _isFlippedRoot_Y)
			{
				//일단 적용하지 말자
				//string prevMat = _matrix_TFResult_World.ToString();
				//_matrix_TFResult_World.RScale((_isFlippedRoot_X ? -1 : 1), (_isFlippedRoot_Y ? -1 : 1));
				//_matrix_TFResult_World.RMultiply(apMatrix.TRS(Vector2.zero, 0.0f, new Vector2((_isFlippedRoot_X ? -1 : 1), (_isFlippedRoot_Y ? -1 : 1))));
				//string nextMat = _matrix_TFResult_World.ToString();

				//Debug.Log("Flipped : " + prevMat + " >> " + nextMat);
			}

			//_matrix_TFResult_WorldWithoutMod.SRMultiply(_matrix_TF_ToParent, true);//ToParent는 넣지 않는다.
			//_matrix_TFResult_WorldWithoutMod.SRMultiply(_matrix_TF_ParentWorld, true);//<<[SR]

			//Without Mod는 계산하지 않았을 경우에만 계산한다.
			//바뀌지 않으므로
			if (!_isCalculateWithoutMod)
			{
				_matrix_TFResult_WorldWithoutMod.RMultiply(_matrix_TF_ToParent);//<<[R]
				_matrix_TFResult_WorldWithoutMod.RMultiply(_matrix_TF_ParentWorld_NonModified);//<<[R]

				//추가 2.25 : Flipped
				if(_isFlippedRoot_X || _isFlippedRoot_Y)
				{
					//여기서 적용하지 말자
					//_matrix_TFResult_WorldWithoutMod.RScale((_isFlippedRoot_X ? -1 : 1), (_isFlippedRoot_Y ? -1 : 1));
					//_matrix_TFResult_World.RMultiply(apMatrix.TRS(Vector2.zero, 0.0f, new Vector2((_isFlippedRoot_X ? -1 : 1), (_isFlippedRoot_Y ? -1 : 1))));
				}

				//리깅용 단축식을 추가한다.
				if (_childMesh != null)
				{
					_vertLocal2MeshWorldMatrix = _matrix_TFResult_WorldWithoutMod.MtrxToSpace;
					_vertLocal2MeshWorldMatrix *= _childMesh._matrix_Vert2Mesh;

					_vertWorld2MeshLocalMatrix = _childMesh._matrix_Vert2Mesh_Inverse;
					_vertWorld2MeshLocalMatrix *= _matrix_TFResult_WorldWithoutMod.MtrxToLowerSpace;

					_vertMeshWorldNoModMatrix = _matrix_TFResult_WorldWithoutMod.MtrxToSpace;
					_vertMeshWorldNoModInverseMatrix = _matrix_TFResult_WorldWithoutMod.MtrxToLowerSpace;
				}

				_isCalculateWithoutMod = true;
			}
			
			//_convert2TargetMatrix4x4 = Matrix4x4.identity;
			//_convert2TargetMatrix3x3 = apMatrix3x3.identity;
			_isCamOrthoCorrection = false;



			#region [미사용 코드] 이전 : 단일 카메라
			//if(_portrait._billboardType != apPortrait.BILLBOARD_TYPE.None && _childMesh != null)
			//{
			//	Camera curCamera = _portrait.GetCamera();
			//	if (curCamera != null && !curCamera.orthographic)
			//	{
			//		//Perspective 카메라라면

			//		Vector3 pos_Cam = curCamera.worldToCameraMatrix.MultiplyPoint3x4(_childMesh.transform.position);
			//		//Vector3 pos_Cam = (curCamera.worldToCameraMatrix * transform.localToWorldMatrix).MultiplyPoint3x4(new Vector3(_matrix_TFResult_World._pos.x, _matrix_TFResult_World._pos.y, 0));
			//		float zDepth = pos_Cam.z;
			//		float zRatio = zDepth / _portrait.GetZDepth();


			//		//Debug.Log("pos_Cam : " + pos_Cam + " / " + zRatio);
			//		//transform.tr//>>TODO
			//		_isCamOrthoCorrection = true;

			//		Matrix4x4 orthoConvertMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zRatio, zRatio, 1));
			//		_convert2TargetMatrix4x4 = _childMesh.transform.worldToLocalMatrix * curCamera.cameraToWorldMatrix * orthoConvertMatrix * curCamera.worldToCameraMatrix * _childMesh.transform.localToWorldMatrix;

			//		_convert2TargetMatrix3x3 = apMatrix3x3.identity;
			//		_convert2TargetMatrix3x3._m00 = _convert2TargetMatrix4x4.m00;
			//		_convert2TargetMatrix3x3._m01 = _convert2TargetMatrix4x4.m01;
			//		_convert2TargetMatrix3x3._m02 = _convert2TargetMatrix4x4.m03;

			//		_convert2TargetMatrix3x3._m10 = _convert2TargetMatrix4x4.m10;
			//		_convert2TargetMatrix3x3._m11 = _convert2TargetMatrix4x4.m11;
			//		_convert2TargetMatrix3x3._m12 = _convert2TargetMatrix4x4.m13;

			//		_convert2TargetMatrix3x3._m20 = _convert2TargetMatrix4x4.m30;
			//		_convert2TargetMatrix3x3._m21 = _convert2TargetMatrix4x4.m31;
			//		_convert2TargetMatrix3x3._m22 = _convert2TargetMatrix4x4.m23;

			//		//Debug.Log("_convert2TargetMatrix4x4 : \n " + _convert2TargetMatrix4x4.ToString() + "\n" + "_convert2TargetMatrix3x3 : \n " + _convert2TargetMatrix3x3.ToString());
			//	}
			//} 
			#endregion

			//변경된 코드 19.9.24 : 여러개의 카메라 지원
			if(_portrait._billboardType != apPortrait.BILLBOARD_TYPE.None && _childMesh != null)
			{
				apOptMainCamera mainCamera = _portrait.GetMainCamera();
				if (mainCamera != null && mainCamera.CameraPersOrthoType == apOptMainCamera.PersOrthoType.Perspective)
				{
					//Perspective 카메라라면

					Vector3 pos_Cam = mainCamera.WorldToCameraMatrix.MultiplyPoint3x4(_childMesh.transform.position);
					float zDepth = pos_Cam.z;
					float zRatio = zDepth / mainCamera.ZDepthToCamera;


					_isCamOrthoCorrection = true;

					Matrix4x4 orthoConvertMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zRatio, zRatio, 1));
					//_convert2TargetMatrix4x4 = _childMesh.transform.worldToLocalMatrix * curCamera.cameraToWorldMatrix * orthoConvertMatrix * curCamera.worldToCameraMatrix * _childMesh.transform.localToWorldMatrix;
					_convert2TargetMatrix4x4 = _childMesh.transform.worldToLocalMatrix 
						//* curCamera.cameraToWorldMatrix 
						* mainCamera.WorldToCameraMatrix.inverse
						* orthoConvertMatrix 
						//* curCamera.worldToCameraMatrix 
						* mainCamera.WorldToCameraMatrix
						* _childMesh.transform.localToWorldMatrix;

					_convert2TargetMatrix3x3 = apMatrix3x3.identity;
					_convert2TargetMatrix3x3._m00 = _convert2TargetMatrix4x4.m00;
					_convert2TargetMatrix3x3._m01 = _convert2TargetMatrix4x4.m01;
					_convert2TargetMatrix3x3._m02 = _convert2TargetMatrix4x4.m03;

					_convert2TargetMatrix3x3._m10 = _convert2TargetMatrix4x4.m10;
					_convert2TargetMatrix3x3._m11 = _convert2TargetMatrix4x4.m11;
					_convert2TargetMatrix3x3._m12 = _convert2TargetMatrix4x4.m13;

					_convert2TargetMatrix3x3._m20 = _convert2TargetMatrix4x4.m30;
					_convert2TargetMatrix3x3._m21 = _convert2TargetMatrix4x4.m31;
					_convert2TargetMatrix3x3._m22 = _convert2TargetMatrix4x4.m23;
				}
			}


			//처리된 TRS
			_updatedWorldPos_NoRequest.x = _matrix_TFResult_World._pos.x;
			_updatedWorldPos_NoRequest.y = _matrix_TFResult_World._pos.y;

			_updatedWorldAngle_NoRequest = _matrix_TFResult_World._angleDeg;

			_updatedWorldScale_NoRequest.x = _matrix_TFResult_World._scale.x;
			_updatedWorldScale_NoRequest.y = _matrix_TFResult_World._scale.y;

			_updatedWorldPos = _updatedWorldPos_NoRequest;
			_updatedWorldAngle = _updatedWorldAngle_NoRequest;
			_updatedWorldScale = _updatedWorldScale_NoRequest;



			//스크립트로 외부에서 제어한 경우
			if (_isExternalUpdate_Position)
			{
				_updatedWorldPos.x = (_exUpdate_Pos.x * _externalUpdateWeight) + (_updatedWorldPos.x * (1.0f - _externalUpdateWeight));
				_updatedWorldPos.y = (_exUpdate_Pos.y * _externalUpdateWeight) + (_updatedWorldPos.y * (1.0f - _externalUpdateWeight));
			}

			if (_isExternalUpdate_Rotation)
			{
				_updatedWorldAngle = (_exUpdate_Angle * _externalUpdateWeight) + (_updatedWorldAngle * (1.0f - _externalUpdateWeight));
			}

			if(_isExternalUpdate_Scaling)
			{ 
				_updatedWorldScale.x = (_exUpdate_Scale.x * _externalUpdateWeight) + (_updatedWorldScale.x * (1.0f - _externalUpdateWeight));
				_updatedWorldScale.y = (_exUpdate_Scale.y * _externalUpdateWeight) + (_updatedWorldScale.y * (1.0f - _externalUpdateWeight));
			}

			if (_isExternalUpdate_Position || _isExternalUpdate_Rotation || _isExternalUpdate_Scaling)
			{
				//WorldMatrix를 갱신해주자
				_matrix_TFResult_World.SetTRS(_updatedWorldPos.x, _updatedWorldPos.y,
										_updatedWorldAngle,
										_updatedWorldScale.x, _updatedWorldScale.y);

				_isExternalUpdate_Position = false;
				_isExternalUpdate_Rotation = false;
				_isExternalUpdate_Scaling = false;
			}
			
			





			//추가 : 소켓도 만들어준다.
			//Vert World를 아직 계산하지 않았지만 Socket 처리에는 문제가 없다.
			if(_socketTransform != null)
			{
				_socketTransform.localPosition = new Vector3(_matrix_TFResult_World._pos.x, _matrix_TFResult_World._pos.y, 0);
				_socketTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, _matrix_TFResult_World._angleDeg);
				_socketTransform.localScale = new Vector3(_matrix_TFResult_World._scale.x, _matrix_TFResult_World._scale.y, 1.0f);
			}

//#if UNITY_EDITOR
//			Profiler.EndSample();
//#endif


			//[MeshUpdate]는 Post Update로 전달

			//3. 자식 호출
			//자식 객체도 업데이트를 한다.
			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].UpdateCalculate_Pre();
				}
			}

		}



		/// <summary>
		/// CalculateStack을 업데이트 한다.
		/// Post-Update이다. Rigging, VertWorld만 처리된다.
		/// </summary>
		public void UpdateCalculate_Post()
		{

//#if UNITY_EDITOR
//			Profiler.BeginSample("Transform - 1. Stack Calculate");
//#endif

			//1. Calculated Stack 업데이트
			if (_calculatedStack != null)
			{
				_calculatedStack.Calculate_Post();
			}

//#if UNITY_EDITOR
//			Profiler.EndSample();
//#endif


//#if UNITY_EDITOR
//			Profiler.BeginSample("Transform - 3. Mesh Update");
//#endif

			////3. Mesh 업데이트 - 중요
			////실제 Vertex의 위치를 적용
			//if (_childMesh != null)
			//{
			//	_childMesh.UpdateCalculate(_calculatedStack.IsRigging,
			//								_calculatedStack.IsVertexLocal,
			//								_calculatedStack.IsVertexWorld,
			//								_isVisible,
			//								_isCamOrthoCorrection);
			//}

			//변경 20.4.2. UpdateCalculate는 뒤로 미루고, Visibility만 먼저 게산해둔다.
			if (_childMesh != null)
			{
				_childMesh.UpdateVisibility(_isVisible);
			}

//#if UNITY_EDITOR
//			Profiler.EndSample();
//#endif

			//3. 자식 호출
			//자식 객체도 업데이트를 한다.
			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].UpdateCalculate_Post();
				}
			}

		}

		//변경 20.4.2 : UpdateCalculate_Post 함수를 분리하여 일부 코드를 여기로 이동시킨다.
		//Visibility 관련하여 Mask Parent/Child의 처리 모순 문제를 해결하기 위함
		//호출 순서를 정리한다.
		//이함수는 UpdateCalculate_Post 함수 직후에 실행되어야 한다.
		public void UpdateMeshes()
		{
			//3. Mesh 업데이트 - 중요
			//실제 Vertex의 위치를 적용
			if (_childMesh != null)
			{
				_childMesh.UpdateCalculate(_calculatedStack.IsRigging,
											_calculatedStack.IsVertexLocal,
											_calculatedStack.IsVertexWorld,
											//_isVisible,
											_isCamOrthoCorrection);
			}

			//3. 자식 호출
			//자식 객체도 업데이트를 한다.
			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].UpdateMeshes();
				}
			}
		}


		




		//본 관련 업데이트 코드
		public void ReadyToUpdateBones()
		{
			//if(!_isBoneUpdatable)
			//{
			//	return;
			//}
			if (_boneList_Root != null)
			{
				for (int i = 0; i < _boneList_Root.Length; i++)
				{
					_boneList_Root[i].ReadyToUpdate(true);
				}
			}

			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].ReadyToUpdateBones();
				}
			}
		}


		public void UpdateBonesWorldMatrix()
		{
			if (_boneList_Root != null)
			{
				//1. World Matrix 처리
				for (int i = 0; i < _boneList_Root.Length; i++)
				{
					_boneList_Root[i].MakeWorldMatrix(true);
				}

				//2. Calculate IK 처리
				for (int i = 0; i < _boneList_Root.Length; i++)
				{
					_boneList_Root[i].CalculateIK(true);
				}

				//3. World Matrix + IK 처리
				for (int i = 0; i < _boneList_Root.Length; i++)
				{
					_boneList_Root[i].MakeWorldMatrixForIK(true, false);
				}

				//추가 : 모든 WorldMatrix를 만들면 이 함수를 호출해야 한다.
				for (int i = 0; i < _boneList_Root.Length; i++)
				{
					_boneList_Root[i].UpdatePostRecursive();
				}
			}

			

			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].UpdateBonesWorldMatrix();
				}
			}
		}
		
		/// <summary>
		/// Bake용 UpdateBonesWorldMatrix() 함수
		/// </summary>
		public void UpdateBonesWorldMatrixForBake()
		{
			if (_boneList_Root != null)
			{
				for (int i = 0; i < _boneList_Root.Length; i++)
				{
					_boneList_Root[i].ResetBoneMatrixForBake(true);
				}
			}

			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].UpdateBonesWorldMatrixForBake();
				}
			}
		}



		//public void RemoveAllCalculateResultParams()
		//{
		//	_calculatedStack.ClearResultParams();
		//	if (_childTransforms != null)
		//	{
		//		for (int i = 0; i < _childTransforms.Length; i++)
		//		{
		//			_childTransforms[i].RemoveAllCalculateResultParams();
		//		}
		//	}
		//}

		public void ResetCalculateStackForBake(bool isRoot)
		{
			if(_calculatedStack != null)
			{
				_calculatedStack.ResetVerticesOnBake();
			}

			//CalResultParam을 모두 삭제한다.

			if (isRoot)
			{
				ClearResultParams(true);
				//RemoveAllCalculateResultParams();
				RefreshModifierLink(true, true);
			}




			if (_childTransforms != null && _childTransforms.Length > 0)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].ReadyToUpdate();
					_childTransforms[i].ReadyToUpdateBones();
				}
			}

			//UpdateBonesWorldMatrix();//>>TODO
			//Bone Matrix를 초기화 하는게 필요하다.
			if (_boneList_Root != null && _boneList_Root.Length > 0)
			{
				for (int i = 0; i < _boneList_Root.Length; i++)
				{
					_boneList_Root[i].ResetBoneMatrixForBake(true);
				}
			}

			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].ResetCalculateStackForBake(false);
				}
			}
		}


		//public void DebugBoneMatrix()
		//{
		//	//if (string.Equals(this.name, "Body"))
		//	//{
		//	//	Debug.Log("Transform Reset");
		//	//	Debug.Log("[ _matrix_TF_ParentWorld : " + _matrix_TF_ParentWorld.ToString() + "]");
		//	//	Debug.Log("[ _matrix_TF_ParentWorld_NonModified : " + _matrix_TF_ParentWorld_NonModified.ToString() + "]");
		//	//	Debug.Log("[ _matrix_TF_ToParent : " + _matrix_TF_ToParent.ToString() + "]");
		//	//	Debug.Log("[ _matrix_TF_LocalModified : " + _matrix_TF_LocalModified.ToString() + "]");
		//	//	Debug.Log("[ _matrix_TFResult_World : " + _matrix_TFResult_World.ToString() + "]");
		//	//	Debug.Log("[ _matrix_TFResult_WorldWithoutMod : " + _matrix_TFResult_WorldWithoutMod.ToString() + "]");
		//	//}

		//	if (_boneList_Root != null && _boneList_Root.Length > 0)
		//	{
		//		for (int i = 0; i < _boneList_Root.Length; i++)
		//		{
		//			_boneList_Root[i].DebugBoneMatrix();
		//		}
		//	}

		//	if (_childTransforms != null)
		//	{
		//		for (int i = 0; i < _childTransforms.Length; i++)
		//		{
		//			_childTransforms[i].DebugBoneMatrix();
		//		}
		//	}
		//}

	

		public void UpdateMaskMeshes()
		{
			if (_childMesh != null)
			{
				_childMesh.RefreshMaskMesh_WithoutUpdateCalculate();
			}
			

			//자식 객체도 업데이트를 한다.
			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].UpdateMaskMeshes();
				}
			}

		}

		// Functions
		//---------------------------------------------------------------
		// 외부 제어 코드를 넣자
		// <Portrait 기준으로 Local Space = Bone 기준으로 World Space 로 설정한다 >
		public void SetPosition(Vector2 worldPosition, float weight = 1.0f)
		{
			_isExternalUpdate_Position = true;
			_externalUpdateWeight = Mathf.Clamp01(weight);
			_exUpdate_Pos = worldPosition;
		}

		public void SetRotation(float worldAngle, float weight = 1.0f)
		{
			_isExternalUpdate_Rotation = true;
			_externalUpdateWeight = Mathf.Clamp01(weight);
			_exUpdate_Angle = worldAngle;
		}

		public void SetScale(Vector2 worldScale, float weight = 1.0f)
		{
			_isExternalUpdate_Scaling = true;
			_externalUpdateWeight = Mathf.Clamp01(weight);
			_exUpdate_Scale = worldScale;
		}

		public void SetTRS(Vector2 worldPosition, float worldAngle, Vector2 worldScale, float weight = 1.0f)
		{
			_isExternalUpdate_Position = true;
			_isExternalUpdate_Rotation = true;
			_isExternalUpdate_Scaling = true;

			_externalUpdateWeight = Mathf.Clamp01(weight);
			_exUpdate_Pos = worldPosition;
			_exUpdate_Angle = worldAngle;
			_exUpdate_Scale = worldScale;
		}


		//12.7 추가
		//Depth가 바뀔때, 그 프레임에서 다시 빌보드를 계산할 필요가 있다.
		public void CorrectBillboardMatrix()
		{
			if (_portrait._billboardType != apPortrait.BILLBOARD_TYPE.None && _childMesh != null)
			{
				#region [미사용 코드] 이전 : 단일 카메라
				//Camera curCamera = _portrait.GetCamera();
				//if (curCamera != null && !curCamera.orthographic)
				//{
				//	//Perspective 카메라라면

				//	Vector3 pos_Cam = curCamera.worldToCameraMatrix.MultiplyPoint3x4(_childMesh.transform.position);
				//	//Vector3 pos_Cam = (curCamera.worldToCameraMatrix * transform.localToWorldMatrix).MultiplyPoint3x4(new Vector3(_matrix_TFResult_World._pos.x, _matrix_TFResult_World._pos.y, 0));
				//	float zDepth = pos_Cam.z;
				//	float zRatio = zDepth / _portrait.GetZDepth();


				//	//Debug.Log("pos_Cam : " + pos_Cam + " / " + zRatio);
				//	//transform.tr//>>TODO
				//	_isCamOrthoCorrection = true;

				//	Matrix4x4 orthoConvertMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zRatio, zRatio, 1));
				//	_convert2TargetMatrix4x4 = _childMesh.transform.worldToLocalMatrix * curCamera.cameraToWorldMatrix * orthoConvertMatrix * curCamera.worldToCameraMatrix * _childMesh.transform.localToWorldMatrix;

				//	_convert2TargetMatrix3x3 = apMatrix3x3.identity;
				//	_convert2TargetMatrix3x3._m00 = _convert2TargetMatrix4x4.m00;
				//	_convert2TargetMatrix3x3._m01 = _convert2TargetMatrix4x4.m01;
				//	_convert2TargetMatrix3x3._m02 = _convert2TargetMatrix4x4.m03;

				//	_convert2TargetMatrix3x3._m10 = _convert2TargetMatrix4x4.m10;
				//	_convert2TargetMatrix3x3._m11 = _convert2TargetMatrix4x4.m11;
				//	_convert2TargetMatrix3x3._m12 = _convert2TargetMatrix4x4.m13;

				//	_convert2TargetMatrix3x3._m20 = _convert2TargetMatrix4x4.m30;
				//	_convert2TargetMatrix3x3._m21 = _convert2TargetMatrix4x4.m31;
				//	_convert2TargetMatrix3x3._m22 = _convert2TargetMatrix4x4.m23;

				//	//Debug.Log("_convert2TargetMatrix4x4 : \n " + _convert2TargetMatrix4x4.ToString() + "\n" + "_convert2TargetMatrix3x3 : \n " + _convert2TargetMatrix3x3.ToString());
				//} 
				#endregion

				//변경
				apOptMainCamera mainCamera = _portrait.GetMainCamera();
				if (mainCamera != null && mainCamera.CameraPersOrthoType == apOptMainCamera.PersOrthoType.Perspective)
				{
					//Perspective 카메라라면
					Vector3 pos_Cam = mainCamera.WorldToCameraMatrix.MultiplyPoint3x4(_childMesh.transform.position);
					//Vector3 pos_Cam = (curCamera.worldToCameraMatrix * transform.localToWorldMatrix).MultiplyPoint3x4(new Vector3(_matrix_TFResult_World._pos.x, _matrix_TFResult_World._pos.y, 0));
					float zDepth = pos_Cam.z;
					float zRatio = zDepth / mainCamera.ZDepthToCamera;


					//Debug.Log("pos_Cam : " + pos_Cam + " / " + zRatio);
					//transform.tr//>>TODO
					_isCamOrthoCorrection = true;

					Matrix4x4 orthoConvertMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zRatio, zRatio, 1));
					//_convert2TargetMatrix4x4 = _childMesh.transform.worldToLocalMatrix * curCamera.cameraToWorldMatrix * orthoConvertMatrix * curCamera.worldToCameraMatrix * _childMesh.transform.localToWorldMatrix;
					_convert2TargetMatrix4x4 = _childMesh.transform.worldToLocalMatrix
						//* curCamera.cameraToWorldMatrix 
						* mainCamera.WorldToCameraMatrix.inverse
						* orthoConvertMatrix
						//* curCamera.worldToCameraMatrix 
						* mainCamera.WorldToCameraMatrix
						* _childMesh.transform.localToWorldMatrix;

					_convert2TargetMatrix3x3 = apMatrix3x3.identity;
					_convert2TargetMatrix3x3._m00 = _convert2TargetMatrix4x4.m00;
					_convert2TargetMatrix3x3._m01 = _convert2TargetMatrix4x4.m01;
					_convert2TargetMatrix3x3._m02 = _convert2TargetMatrix4x4.m03;

					_convert2TargetMatrix3x3._m10 = _convert2TargetMatrix4x4.m10;
					_convert2TargetMatrix3x3._m11 = _convert2TargetMatrix4x4.m11;
					_convert2TargetMatrix3x3._m12 = _convert2TargetMatrix4x4.m13;

					_convert2TargetMatrix3x3._m20 = _convert2TargetMatrix4x4.m30;
					_convert2TargetMatrix3x3._m21 = _convert2TargetMatrix4x4.m31;
					_convert2TargetMatrix3x3._m22 = _convert2TargetMatrix4x4.m23;
					
				}
			}
		}


		// Editor Functions
		//------------------------------------------------
		public void Bake(apPortrait portrait, //apMeshGroup srcMeshGroup, 
							apOptTransform parentTransform,
							apOptRootUnit rootUnit,
							string name,
							int transformID, int meshGroupUniqueID, apMatrix defaultMatrix,
							bool isMesh, int level, int depth,
							bool isVisible_Default,
							Color meshColor2X_Default,
							float zScale,
							bool isUseModMeshSet
										)
		{
			_portrait = portrait;
			_rootUnit = rootUnit;
			_transformID = transformID;
			_name = name;
			_meshGroupUniqueID = meshGroupUniqueID;

			_parentTransform = parentTransform;

			_defaultMatrix = new apMatrix(defaultMatrix);
			_transform = transform;

			_level = level;
			
			_depth = depth;

			_isVisible_Default = isVisible_Default;
			_meshColor2X_Default = meshColor2X_Default;

			float depthZPos = 0.0f;
			if (parentTransform != null)
			{
				//_depth -= parentTransform._depth;
				depthZPos = (float)(_depth - parentTransform._depth) * zScale;
			}
			else
			{
				depthZPos = (float)_depth * zScale;
			}

			//이부분 실험 중
			//1. Default Matrix를 Transform에 적용하고, Modifier 계산에서는 제외하는 경우
			//결과 : Bake시에는 "Preview"를 위해서 DefaultMatrix 위치로 이동을 시키지만, 실행시에는 원점으로 이동시킨다.
			//_transform.localPosition = _defaultMatrix.Pos3 - new Vector3(0.0f, 0.0f, (float)_depth);
			//_transform.localRotation = Quaternion.Euler(0.0f, 0.0f, _defaultMatrix._angleDeg);
			//_transform.localScale = _defaultMatrix._scale;

			//2. Default Matrix를 Modifier에 포함시키고 Transform은 원점인 경우 (Editor와 동일)
			_transform.localPosition = -new Vector3(0.0f, 0.0f, depthZPos);
			_transform.localRotation = Quaternion.identity;
			_transform.localScale = Vector3.one;

			if (isMesh)
			{
				_unitType = UNIT_TYPE.Mesh;
			}
			else
			{
				_unitType = UNIT_TYPE.Group;
			}

			_childTransforms = null;
			_childMesh = null;

			//추가 19.5.25 : v1.1.7부터는 ModMeshSet을 사용하도록 하자
			_isUseModMeshSet = isUseModMeshSet;

			//추가 : Bake 직후에는 Calculate Stack Result를 null로 날리자.
			_calculatedStack = null;
		}

		public void BakeModifier(apPortrait portrait, apMeshGroup srcMeshGroup, bool isUseModMesh)
		{
			if (srcMeshGroup != null)
			{
				_modifierStack.Bake(srcMeshGroup._modifierStack, portrait, isUseModMesh);
			}
		}

		public void SetChildMesh(apOptMesh optMesh)
		{
			_childMesh = optMesh;
		}

		public void AddChildTransforms(apOptTransform childTransform)
		{
			if (_childTransforms == null)
			{
				_childTransforms = new apOptTransform[1];
				_childTransforms[0] = childTransform;
			}
			else
			{
				apOptTransform[] nextTransform = new apOptTransform[_childTransforms.Length + 1];
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					nextTransform[i] = _childTransforms[i];
				}
				nextTransform[nextTransform.Length - 1] = childTransform;

				_childTransforms = new apOptTransform[nextTransform.Length];
				for (int i = 0; i < nextTransform.Length; i++)
				{
					_childTransforms[i] = nextTransform[i];
				}
			}
		}

		public void ClearResultParams(bool isRecursive)
		{
			if (_calculatedStack == null)
			{
				_calculatedStack = new apOptCalculatedResultStack(this);
			}

			//Debug.Log("Clear Param : " + _transformID);
			_calculatedStack.ClearResultParams();
			_modifierStack.ClearAllCalculateParam();

			if(isRecursive)
			{
				if(_childTransforms != null && _childTransforms.Length > 0)
				{
					for (int i = 0; i < _childTransforms.Length; i++)
					{
						_childTransforms[i].ClearResultParams(true);
					}
				}
			}
			
		}

		/// <summary>
		/// [핵심 코드]
		/// Modifier를 업데이트할 수 있도록 연결해준다.
		/// </summary>
		public void RefreshModifierLink(bool isRecursive, bool isRoot)
		{
			if (_calculatedStack == null)
			{
				_calculatedStack = new apOptCalculatedResultStack(this);
			}

			_modifierStack.Link(_portrait, this);
			if(isRecursive)
			{
				if (_childTransforms != null && _childTransforms.Length > 0)
				{
					for (int i = 0; i < _childTransforms.Length; i++)
					{
						_childTransforms[i].RefreshModifierLink(true, false);
					}
				}
			}

			if (isRoot)
			{
				_modifierStack.LinkModifierStackToRenderUnitCalculateStack(true, this, isRecursive);
			}
			
		}



		/// <summary>
		/// [핵심 코드]
		/// Modifier를 업데이트할 수 있도록 연결해준다.
		/// </summary>
		public IEnumerator RefreshModifierLinkAsync(bool isRecursive, bool isRoot, apAsyncTimer asyncTimer)
		{
			if (_calculatedStack == null)
			{
				_calculatedStack = new apOptCalculatedResultStack(this);
			}

			if(asyncTimer.IsYield())
			{
				yield return asyncTimer.WaitAndRestart();
			}

			yield return _modifierStack.LinkAsync(_portrait, this, asyncTimer);

			if(isRecursive)
			{
				if (_childTransforms != null && _childTransforms.Length > 0)
				{
					for (int i = 0; i < _childTransforms.Length; i++)
					{
						yield return _childTransforms[i].RefreshModifierLinkAsync(true, false, asyncTimer);
					}
				}
			}

			if (isRoot)
			{
				_modifierStack.LinkModifierStackToRenderUnitCalculateStack(true, this, isRecursive);
			}
			
		}



		// Functions
		//------------------------------------------------
		public void Show(bool isChildShow)
		{
			if (_childMesh != null)
			{
				_childMesh.Show(true);
			}

			if (isChildShow)
			{
				if (_childTransforms != null)
				{
					for (int i = 0; i < _childTransforms.Length; i++)
					{
						_childTransforms[i].Show(true);
					}
				}
			}
		}




		public void Hide(bool isChildHide)
		{
			if (_childMesh != null)
			{
				_childMesh.Hide();
			}

			if (isChildHide)
			{
				if (_childTransforms != null)
				{
					for (int i = 0; i < _childTransforms.Length; i++)
					{
						_childTransforms[i].Hide(true);
					}
				}
			}
		}


		public void ShowWhenBake(bool isChildShow)
		{
			if (_childMesh != null)
			{
				_childMesh.SetVisibleByDefault();
			}

			if (isChildShow)
			{
				if (_childTransforms != null)
				{
					for (int i = 0; i < _childTransforms.Length; i++)
					{
						_childTransforms[i].ShowWhenBake(true);
					}
				}
			}
		}

		public void ResetCommandBuffer(bool isRegistToCamera)
		{
			if (_childMesh != null)
			{
				if (isRegistToCamera)
				{
					_childMesh.ResetMaskParentSetting();
				}
				else
				{
					//_childMesh.CleanUpMaskParent();//기존
					_childMesh.ClearCameraData();//변경
				}
			}
			if (_childTransforms != null)
			{
				for (int i = 0; i < _childTransforms.Length; i++)
				{
					_childTransforms[i].ResetCommandBuffer(isRegistToCamera);
				}
			}
		}

		//추가 2.25 : Root OptTransform인 경우, apPortrait가 Flipped된 상태인지 값을 받아야 한다.
		public void SetRootFlipped(bool isFlippedX, bool isFlippedY)
		{
			_isFlippedRoot_X = isFlippedX;
			_isFlippedRoot_Y = isFlippedY;
			//Debug.LogError("[" + this.name + "] : Flipped / X : " + isFlippedX + " / Y : " + isFlippedY);
		}

		// Get / Set
		//------------------------------------------------
		public apOptModifierUnitBase GetModifier(int uniqueID)
		{
			return _modifierStack.GetModifier(uniqueID);
		}



		public apOptTransform GetMeshTransform(int uniqueID)
		{
			for (int i = 0; i < _childTransforms.Length; i++)
			{
				if (_childTransforms[i]._unitType == UNIT_TYPE.Mesh
					&& _childTransforms[i]._transformID == uniqueID)
				{
					return _childTransforms[i];
				}
			}
			return null;
		}

		public apOptTransform GetMeshGroupTransform(int uniqueID)
		{
			for (int i = 0; i < _childTransforms.Length; i++)
			{
				if (_childTransforms[i]._unitType == UNIT_TYPE.Group
					&& _childTransforms[i]._transformID == uniqueID)
				{
					return _childTransforms[i];
				}
			}
			return null;
		}


		public apOptTransform GetMeshTransformRecursive(int uniqueID)
		{
			apOptTransform result = GetMeshTransform(uniqueID);
			if (result != null)
			{
				return result;
			}

			apOptTransform curGroupTransform = null;
			for (int i = 0; i < _childTransforms.Length; i++)
			{
				curGroupTransform = _childTransforms[i];
				if (curGroupTransform._unitType != UNIT_TYPE.Group)
				{
					continue;
				}

				result = curGroupTransform.GetMeshTransformRecursive(uniqueID);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}

		public apOptTransform GetMeshGroupTransformRecursive(int uniqueID)
		{
			apOptTransform result = GetMeshGroupTransform(uniqueID);
			if (result != null)
			{
				return result;
			}

			apOptTransform curGroupTransform = null;
			for (int i = 0; i < _childTransforms.Length; i++)
			{
				curGroupTransform = _childTransforms[i];
				if (curGroupTransform._unitType != UNIT_TYPE.Group)
				{
					continue;
				}

				result = curGroupTransform.GetMeshGroupTransformRecursive(uniqueID);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}

		public apOptBone GetBone(int uniqueID)
		{
			if (_boneList_All != null)
			{
				//이전
				//for (int i = 0; i < _boneList_All.Length; i++)
				//{
				//	if (_boneList_All[i]._uniqueID == uniqueID)
				//	{
				//		return _boneList_All[i];
				//	}
				//}

				//변경 19.5.23 : 첫 요청이 들어오면 ID > Bone 부터 만들자
				if(_boneID2Bone == null || _boneID2Bone.Count != _boneList_All.Length)
				{
					_boneID2Bone = new Dictionary<int, apOptBone>();

					for (int i = 0; i < _boneList_All.Length; i++)
					{
						_boneID2Bone.Add(_boneList_All[i]._uniqueID, _boneList_All[i]);
					}
				}
				
				if(_boneID2Bone.ContainsKey(uniqueID))
				{
					return _boneID2Bone[uniqueID];
				}
			}

			return null;
		}

		public apOptBone GetBoneRecursive(int uniqueID)
		{
			if (_boneList_All != null)
			{
				//이전
				//for (int i = 0; i < _boneList_All.Length; i++)
				//{
				//	if (_boneList_All[i]._uniqueID == uniqueID)
				//	{
				//		return _boneList_All[i];
				//	}
				//}

				//변경 19.5.23 : 첫 요청이 들어오면 ID > Bone 부터 만들자
				if(_boneID2Bone == null || _boneID2Bone.Count != _boneList_All.Length)
				{
					_boneID2Bone = new Dictionary<int, apOptBone>();

					for (int i = 0; i < _boneList_All.Length; i++)
					{
						_boneID2Bone.Add(_boneList_All[i]._uniqueID, _boneList_All[i]);
					}
				}
				
				if(_boneID2Bone.ContainsKey(uniqueID))
				{
					return _boneID2Bone[uniqueID];
				}
			}


			apOptBone result = null;
			if(_childTransforms == null)
			{
				//Debug.LogError("Child가 없다. [Opt Transform : " + _name + "]");
				return null;
			}
			for (int i = 0; i < _childTransforms.Length; i++)
			{
				result = _childTransforms[i].GetBoneRecursive(uniqueID);
				if (result != null)
				{
					return result;
				}
			}

			return null;
		}


		//추가 12.5 : Extra Option에 의해 Depth가 바뀐 경우 예외적으로 해당 MeshGroup에 호출해야한다.
		//이 MeshGroup은 멤버와 다른 선택된 MeshGroup을 대상으로 한다.
		public void SetExtraDepthChangedEvent(FUNC_EXTRA_DEPTH_CHANGED func_ExtraDepthChanged)
		{
			_func_ExtraDepthChanged = func_ExtraDepthChanged;
			//TODO : 이건 RootUnit에서 제어하는 걸로 하자
		}


		//추가 12.4 : 변경된 텍스쳐에 대한 정보
		public bool IsExtraTextureChanged
		{
			get
			{
				return _isExtraTextureChanged && _extraTextureData != null;
			}
		}
		public apOptTextureData ChangedExtraTextureData
		{
			get
			{
				return _extraTextureData;
			}
		}
	}
}