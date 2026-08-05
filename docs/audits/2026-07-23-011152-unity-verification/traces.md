# Verification traces

## V-CLI

Shell `PATH` resolves `unity` to `/Users/abrahamrubio/.unity/bin/unity` → CLI reports version `1.0.0-beta.2` → `unity status` connects to CrazyMarket at port `7800` → Editor reports `ready` on Unity `6000.4.1f1`.

## V-COMPILE

`unity command recompile --focus false` → Editor reports no scripts need recompilation → `recompile_status` returns `up_to_date`, `failed: false`, `errors: []`.

## V-TESTS

`unity command list_tests --mode all` → Unity Test Framework reports `Count: 0` and `Found 0 test(s)`.

## V-CONSOLE

`unity command get_console_logs --severity error --limit 200` → one unrelated Package Manager authentication error → no captured script compilation errors.
