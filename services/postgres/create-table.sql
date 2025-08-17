CREATE TABLE "tb_payment" (
    "correlation_id" VARCHAR(50) NOT NULL,
    "amount" decimal NOT NULL,
    "processor" INTEGER NOT NULL,
    "processor_at" timestamp NOT NULL
);