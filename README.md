# VR Dome

Unity 돔 영상 확장 실험 프로젝트입니다. 메인 영상의 일부 영역을 BG에 부드럽게 연결하고, 장면에 맞춘 파티클을 함께 표시합니다.

- Unity: **6000.6.9f1**, URP
- VR 대상: **Meta Quest 3 + Windows PC**, Meta Horizon Link / OpenXR / Direct3D 11
- PC·VR 공용 씬: `Assets/Scenes/quest3_dome.unity`
- 모니터 프리뷰: `Assets/Scenes/nov11_video_suite.unity`
- 영상 프리셋: `Assets/DomeClipPresets`
- 사용 및 검증 안내: `Docs/Quest3/Quest3_사용안내.txt`

## 프로젝트 열기

Git LFS가 필요합니다. Fork 또는 GitHub Desktop으로 복제한 뒤 LFS 영상도 내려받고 Unity Hub에서 프로젝트 폴더를 엽니다.

```sh
git lfs install
git clone https://github.com/naaaaaaaJJan/vrdome.git
cd vrdome
git lfs pull
```

`Library`, `Temp`, 로그, 로컬 편집기 설정 및 APK는 저장소에서 제외합니다. Unity가 캐시를 다시 생성하므로 첫 실행은 시간이 걸릴 수 있습니다. 영상은 Git LFS로 추적하고 `.meta` 파일은 소스와 함께 유지합니다.

## Unity Play (HMD 없이 사용)

`quest3_dome` 씬에서 Play를 누르면 PC 조작으로 자동 전환됩니다. Game 탭을 클릭해 키보드 포커스를 준 뒤 사용합니다.

- 우클릭 드래그: 시점 이동
- 왼쪽 클릭: 공간 메뉴 버튼 선택
- Tab / 우측 상단 버튼: 메뉴 표시·숨김 (메뉴를 다시 열면 정면에 배치)
- 좌/우 방향키: 이전/다음 영상
- 위/아래 방향키: 다음/이전 좌석
- Space: 재생/일시정지, R: 시점 초기화, Esc: 메뉴 숨김

실행 중인 XR 화면이 있으면 VR 조작을 사용합니다. 컨트롤러 또는 머리 추적의 일시적인 손실로 PC 모드가 되지는 않습니다. PC와 VR은 동일한 영상·BG·파티클·좌석을 공유합니다.

## Windows PC + Quest Link

1. Windows에서 Unity 6000.6.9f1로 프로젝트를 열고 Git LFS 영상을 받습니다.
2. File > Build Profiles에서 Windows를 선택하고 Switch Platform 합니다.
3. Tools > Dome Study > PC VR > Configure Windows Link를 실행합니다.
4. Meta Horizon Link 앱에서 Quest 3를 USB Link 또는 Air Link로 연결합니다.
5. Link 앱 설정의 OpenXR Runtime에서 Meta Horizon Link를 활성 런타임으로 설정합니다.
6. 헤드셋 안에서도 Link 연결 화면으로 들어간 뒤 quest3_dome 씬을 Play 합니다.

Link는 Play 전에 연결하세요. 연결 후에도 PC 모드라면 Play를 정지했다가 다시 실행하세요.
PC XR Plug-in Management에 OpenXR 및 Initialize XR on Startup이 켜지고 Oculus Touch Controller Profile이 설정됩니다.
Mac의 Play는 모니터 미리보기용입니다. Quest Link 송출은 Windows에서 실행합니다. Android/APK는 필요 없습니다.

실행 파일은 Tools > Dome Study > PC VR > Build Windows EXE로 만듭니다.
결과: Builds/Windows/VRDome.exe. exe만 복사하지 말고 Windows 폴더 전체를 옮깁니다.
Windows 빌드 모듈이 필요한 경우 Unity Hub에서 설치합니다.

- 오른손 B: 메뉴 표시/숨김
- 트리거: 포인터로 가리킨 버튼 선택
- 왼쪽 스틱 좌우: 메뉴가 열린 상태에서 영상 전환
- 오른손 A: 재생/일시정지
- 왼손 X: 현재 좌석 시점 재설정

14개 영상 프리셋 전환 시 메인, BG, 파티클 설정이 함께 바뀝니다. 일반 2D 원본 영상은 곡면에 표시되며, 극장과 파티클은 3D 공간에 배치됩니다.

기존 에디터 메뉴/영상/좌석 검사는 통과했습니다. Windows EXE 빌드와 실제 Link 양안 표시, 컨트롤러 입력 및 프레임레이트는 Windows PC와 기기에서 검증해야 합니다. Main / Abstract / Context 실험 조건 배정과 결과 기록은 별도 구현 대상입니다.

이 저장소에는 연구용 영상 및 생성 이미지가 포함됩니다. 별도의 일괄 재배포 라이선스를 부여하지 않습니다.
