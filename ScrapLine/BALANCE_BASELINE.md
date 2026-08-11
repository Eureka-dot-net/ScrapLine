# Baseline economy

These values are the initial vertical-slice playtest baseline. They optimize for a readable first
session, not permanent live-service balance.

## Opening budget

A new game starts with **250 credits** and one free **Can Bale**. A functional raw-selling line costs
150 credits: one Spawner (50), five Conveyors (50), and one Seller (50). The remaining 100 credits are
the mistake buffer or the exact cost of the first Shredder.

## Pricing rules

- Paid waste bales cost **80% of their raw contents' sale value**. Raw selling therefore remains
  profitable, but only at a 20% gross margin on contents value.
- Every recipe's output sale value must be greater than its inputs' sale value. EditMode validation
  enforces both this rule and the waste-bale formula.
- Machine prices gate the production tiers: Conveyor 10, Spawner 50, Seller 50, Shredder 100,
  Granulator 125, Sorter 150, Plate Press 250, and Fabricator 500.

## Values and returns

| Step | Time | Input value | Output value | Value uplift | Theoretical uplift/minute |
| --- | ---: | ---: | ---: | ---: | ---: |
| Can -> shredded aluminum | 5s | 2 | 6 | 4 | 48 |
| Plastic bottle -> granulated plastic | 10s | 2 | 6 | 4 | 24 |
| Shredded aluminum -> aluminum plate | 10s | 6 | 16 | 10 | 60 |
| Plate + 5 granulated plastic -> reinforced panel | 5s | 46 | 75 | 29 | 348* |

\*The Fabricator's practical rate is constrained by upstream supply. With one Granulator, five units
take 50 seconds, reducing the panel step to about 34.8 credits of uplift per minute and giving the
500-credit Fabricator a roughly 14.4-minute marginal payback.

First-stage machine payback from value uplift is approximately 2.1 minutes for the Shredder and 5.2
minutes for the Granulator at continuous utilization. The 250-credit Plate Press pays back in about
4.2 minutes at continuous utilization. Conveyors, Spawners, Sellers, and Sorters enable throughput and
routing, so they do not have a standalone conversion payback.

## Waste bales

| Bale | Contents | Raw value | Paid cost | First-stage processed value |
| --- | --- | ---: | ---: | ---: |
| Can Bale | 25 cans | 50 | 40 | 150 |
| Plastic Bale | 25 bottles | 50 | 40 | 150 |
| Mixed Bale | 25 cans + 25 bottles | 100 | 80 | 300 |
| Bulk Mixed Bale | 50 cans + 50 bottles | 200 | 160 | 600 |

The first Can Bale is granted free only when creating a new game. Existing saves retain their credits
and queue contents.
