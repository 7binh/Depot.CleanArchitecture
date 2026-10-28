import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const WIDTH = 140;
const HEIGHT = 28;
/** Room around the line so the end dot is not cut off. */
const PAD = 2;

/** A thin line over a few values with a dot on the last one: no axes, no grid, no fill. */
@Component({
  selector: 'app-sparkline',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      role="img"
      class="block"
      [attr.viewBox]="'0 0 ' + width + ' ' + height"
      [attr.width]="width"
      [attr.height]="height"
      [attr.aria-label]="label()"
    >
      <polyline
        fill="none"
        class="stroke-ink-body"
        stroke-width="1.25"
        stroke-linecap="round"
        stroke-linejoin="round"
        [attr.points]="line()"
      />
      <circle class="fill-brand" r="2" [attr.cx]="last().x" [attr.cy]="last().y" />
    </svg>
  `,
})
export class Sparkline {
  readonly values = input.required<number[]>();
  readonly label = input.required<string>();

  protected readonly width = WIDTH;
  protected readonly height = HEIGHT;

  private readonly points = computed(() => {
    const values = this.values();
    const peak = Math.max(...values, 1);
    const step = values.length > 1 ? (WIDTH - PAD * 2) / (values.length - 1) : 0;
    return values.map((value, index) => ({
      x: PAD + index * step,
      y: PAD + (HEIGHT - PAD * 2) * (1 - value / peak),
    }));
  });

  protected readonly line = computed(() =>
    this.points()
      .map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`)
      .join(' '),
  );
  protected readonly last = computed(() => this.points().at(-1) ?? { x: PAD, y: HEIGHT - PAD });
}
