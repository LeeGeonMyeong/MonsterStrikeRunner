# 실습 1 — Brotli + Decompression Fallback 빌드 결과

> 아래는 최초 복사 시점의 기록이다. 이후 새 Unity 빌드로 `web/`와 `docs/`를 교체했다. 최신 용량은 **12,190,211바이트(12.190 MB)**이며, 상세 결과는 [Build Report 실습](build-report-exercise.md)을 참고한다.

확인일: 2026-11-04

## 1. 압축 설정 확인

- Brotli 압축 적용: WASM, 데이터, 프레임워크 파일의 헤더에서 `UnityWeb Compressed Content (brotli)`를 확인했다.
- Decompression Fallback 적용: 로더에 `BrotliDecompressBuffer` 디코더가 포함되어 있다.

## 2. 저장소 루트 web 폴더 준비

기존 `docs/` 빌드를 `web/`에 복사했다. 새로 Unity 빌드를 실행해 생성한 결과가 아니다. 총 8개 파일의 SHA-256을 비교하여 원본과 복사본이 모두 일치함을 확인했다. 기존 파일명은 해시 기반이므로 이미지의 예시 파일명과 다르다.

진입 파일: `web/index.html`. HTTP 서버를 통해 실행한다.

## 3. web/Build 확장자 및 최대 파일 크기 확인

| 파일 | 크기(바이트) | 크기(MB) |
| --- | ---: | ---: |
| b1d36b92a045e25ccc8c354851fc680d.wasm.unityweb | 6,177,841 | 6.178 |
| 68455881a828a4b8cdd29157c005f491.data.unityweb | 5,722,365 | 5.722 |
| af08e474a66d0e38ae512615ef9f4eb3.framework.js.unityweb | 75,164 | 0.075 |
| a880c6ede2354ba310c2f9c76c6ceb1c.loader.js | 117,894 | 0.118 |

압축 대상 3개 파일은 모두 `.unityweb`으로 끝난다. 로더는 실습 이미지와 동일하게 `.loader.js`로 끝난다.

가장 큰 파일은 WASM으로 **6,177,841바이트(6.178 MB, 5.892 MiB)**이며, **25 MB(25,000,000바이트) 미만**이다.

## 4. web 폴더 전체 용량 기록

숨김 파일 `.nojekyll`을 포함한 총 8개 파일의 논리적 크기 합계는 **12,124,127바이트 = 12.124 MB = 11.562 MiB**이다. 디스크 할당 크기는 포함하지 않는다.

기존 빌드에 코드·텍스처 최적화가 적용되어 있으므로 이 수치를 '최적화 전' 용량으로 표시하지 않는다. 이후 추가 최적화와 비교할 현재 기준 용량으로 사용한다.
