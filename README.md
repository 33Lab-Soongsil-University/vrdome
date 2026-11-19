# VR Dome

Unity 돔 영상 확장 실험 프로젝트입니다. 메인 영상의 일부 영역을 BG에 부드럽게 연결하고, 장면에 맞춘 파티클을 함께 표시합니다.

- Unity: **6000.6.9f1**, URP
- VR 대상: **Meta Quest 3**, Android / OpenXR / ARM64 / IL2CPP
- VR 씬: `Assets/Scenes/quest3_dome.unity`
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

## Quest 3

`Tools > Dome Study > Quest 3 > Build APK`에서 빌드합니다. 결과는 `Builds/Quest3/VRDome-Quest3.apk`입니다.

- 오른손 B: 메뉴 표시/숨김
- 트리거: 포인터로 가리킨 버튼 선택
- 왼쪽 스틱 좌우: 메뉴가 열린 상태에서 영상 전환
- 오른손 A: 재생/일시정지
- 왼손 X: 현재 좌석 시점 재설정

14개 영상 프리셋 전환 시 메인, BG, 파티클 설정이 함께 바뀝니다. 일반 2D 원본 영상은 곡면에 표시되며, 극장과 파티클은 3D 공간에 배치됩니다.

Android APK 빌드 및 에디터 메뉴/영상/좌석 검사를 통과했습니다. 실제 HMD의 양안 표시, 컨트롤러 입력, Android 영상 디코딩과 프레임레이트는 기기 검증이 필요합니다. Main / Abstract / Context 실험 조건 배정과 결과 기록은 별도 구현 대상입니다.

이 저장소에는 연구용 영상 및 생성 이미지가 포함됩니다. 별도의 일괄 재배포 라이선스를 부여하지 않습니다.
