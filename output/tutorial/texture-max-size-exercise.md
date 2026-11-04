# 실습 5 — 텍스처 Max Size 조정

기록일: 2026-11-04

Build Report 상위 이미지 중 프로젝트에서 관리하는 배경과 캐릭터 프레임 시트의 WebGL Override Max Size를 변경했다. 첨부 슬라이드가 참조하는 4.4 기준표는 제공되지 않아 512를 시험값으로 적용했다. Unity 기본 로고와 패키지 에셋은 수정하지 않았다.

| 이미지 | 이전 Web Max Size | 변경 후 |
| --- | ---: | ---: |
| Assets/Resources/Backgrounds/ForestRoad.png | 1024 | 512 |
| Assets/Resources/Characters/RunnerHeroFrames.png | 1024 | 512 |
| Assets/Resources/Characters/RunnerMonsterFrames.png | 1024 | 512 |

원본 PNG와 기본 플랫폼 설정은 유지하고 WebGL Override만 변경했다. Unity가 변경된 meta를 다시 임포트하도록 했다. RunnerWebBuild.cs에서도 동일한 값을 사용하여 다음 빌드가 1024로 되돌리지 않도록 했다. 기존 단일 캐릭터 이미지의 256 설정과 텍스처 포맷은 유지했다.

## Game 뷰 확인

활성 빌드 대상 Web에서 Play를 실행했다. Game 뷰를 최대화하고 480×853, 1×로 시작 화면·조작 연습·게임 화면 및 결과 화면을 확인했다. 배경 타일, 플레이어와 몬스터의 형태가 표시되는 크기에서 구분되어 512 설정을 유지했다. 실제 휴대폰에서의 화질 평가는 아직 수행하지 않았다. 확인 후 Play를 종료했다.

## 빌드 상태

이번 단계는 임포트 설정 조정과 Game 뷰 확인이다. 기존 web/docs 산출물은 직전 Build Report 실습의 빌드이며, 이번 512 설정을 반영한 재빌드·용량 측정·푸시·배포는 아직 수행하지 않았다. 이전 보고서의 텍스처 88.1% 및 web 12.190 MB는 변경 전 기준값으로 보관한다.
