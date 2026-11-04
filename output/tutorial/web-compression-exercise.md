# Brotli + Decompression Fallback 실습

- 설정: Brotli, Decompression Fallback 활성화.
- Unity 메뉴: `Runner > Build Web for Compression Exercise`.
- 출력: 저장소 루트 `web/` (기존 GitHub Pages 빌드 메뉴는 `docs/` 사용).
- 기존 코드/텍스처 최적화 설정을 유지하므로, 이 결과를 '최적화 전' 기준으로 해석하지 않는다.

## 이번 실행 결과

2026-11-04: Unity 6000.3.11f1 및 README에 기재된 6000.3.23f1에서 배치 빌드를 시도했으나, 프로젝트 경로를 읽은 직후 종료 코드 1로 종료했다. 빌드 메서드 실행 및 새 `web/` 출력은 확인되지 않았다. 로그에 구체적 실패 원인이 없어 새 빌드의 용량 검증은 미완료다.

로그: `output/web-exercise-build.log`, `output/web-exercise-build-6000.3.23.log`.

## 참고: 기존 docs/Build 산출물

아래는 이번에 생성한 빌드가 아닌 기존 파일의 실측값이다.

| 종류 | 바이트 | MiB |
| --- | ---: | ---: |
| wasm.unityweb | 6,177,841 | 5.89 |
| data.unityweb | 5,722,365 | 5.46 |
| framework.js.unityweb | 75,164 | 0.072 |
| loader.js | 117,894 | 0.112 |

기존 Build 폴더의 가장 큰 파일은 25 MB(25,000,000바이트)보다 작다. 새 web 폴더의 총용량은 빌드 성공 후 별도로 기록해야 한다.
