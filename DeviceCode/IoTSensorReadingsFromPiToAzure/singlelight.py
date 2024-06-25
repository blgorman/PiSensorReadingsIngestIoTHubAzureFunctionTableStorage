import sys
import time

try:
    # Transitional fix for breaking change in LTR559
    from ltr559 import LTR559
    ltr559 = LTR559()
except ImportError:
    import ltr559

time.sleep(1.0)

try:
        lux = ltr559.get_lux()
        prox = ltr559.get_proximity()
        output = """'Light':'{:05.02f} Lux', 'Proximity':'{:05.02f}'""".format(lux, prox)
        sys.stdout.write(output)

except KeyboardInterrupt:
    pass